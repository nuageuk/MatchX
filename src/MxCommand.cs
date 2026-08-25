using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace MatchX
{
    public class MxCommand
    {
        private static ObjectId _sourceId = ObjectId.Null;
        private static ObjectId _sourceSpaceId = ObjectId.Null;

        [CommandMethod("MX")]
        public static void Mx()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            if (_sourceId.IsNull || !_sourceId.IsValid || _sourceId.IsErased)
            {
                PromptEntityOptions peo = new PromptEntityOptions("\nMatchX - select source entity: ");
                PromptEntityResult per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK) return;

                _sourceId = per.ObjectId;
                _sourceSpaceId = CurrentSpaceId(db);

                ed.WriteMessage("\nMatchX - source captured. Run MX again and select target entities. Run MXRESET to pick a new source.");
                return;
            }

            PromptSelectionOptions pso = new PromptSelectionOptions
            {
                MessageForAdding = "\nMatchX - select target entities: "
            };
            PromptSelectionResult psr = ed.GetSelection(pso);
            if (psr.Status != PromptStatus.OK) return;

            ObjectId[] targetIds = psr.Value.GetObjectIds();
            if (targetIds.Length == 0) return;

            ObjectId targetSpaceId = CurrentSpaceId(db);

            if (targetSpaceId == _sourceSpaceId)
            {
                ApplyNative(doc, _sourceId, targetIds);
            }
            else
            {
                ApplyWithClone(doc, _sourceId, targetSpaceId, targetIds);
            }
        }

        [CommandMethod("MXRESET")]
        public static void MxReset()
        {
            _sourceId = ObjectId.Null;
            _sourceSpaceId = ObjectId.Null;

            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage("\nMatchX - source cleared.");
        }

        private static void ApplyNative(Document doc, ObjectId sourceId, ObjectId[] targetIds)
        {
            Editor ed = doc.Editor;

            List<ObjectId> pickSet = new List<ObjectId> { sourceId };
            pickSet.AddRange(targetIds);

            ed.SetImpliedSelection(pickSet.ToArray());
            doc.SendStringToExecute("_.MATCHPROP \n\n", true, false, false);
        }

        private static void ApplyWithClone(Document doc, ObjectId sourceId, ObjectId targetSpaceId, ObjectId[] targetIds)
        {
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ObjectId cloneId = CloneEntityIntoSpace(db, sourceId, targetSpaceId);
            if (cloneId.IsNull)
            {
                ed.WriteMessage("\nMatchX - failed to clone source entity into the target layout.");
                return;
            }

            SelectCloneAsPickSet(ed, cloneId, targetIds);
            ScheduleCloneErasure(doc, cloneId);

            doc.SendStringToExecute("_.MATCHPROP \n\n", true, false, false);
        }

        private static ObjectId CloneEntityIntoSpace(Database db, ObjectId sourceId, ObjectId targetSpaceId)
        {
            ObjectIdCollection idsToClone = new ObjectIdCollection { sourceId };
            IdMapping mapping = new IdMapping();

            db.DeepCloneObjects(idsToClone, targetSpaceId, mapping, false);

            if (mapping.Contains(sourceId))
            {
                IdPair pair = mapping[sourceId];
                if (pair.IsCloned)
                {
                    return pair.Value;
                }
            }

            return ObjectId.Null;
        }

        private static void SelectCloneAsPickSet(Editor ed, ObjectId cloneId, ObjectId[] targetIds)
        {
            List<ObjectId> pickSet = new List<ObjectId> { cloneId };
            pickSet.AddRange(targetIds);

            ed.SetImpliedSelection(pickSet.ToArray());
        }

        private static void ScheduleCloneErasure(Document doc, ObjectId cloneId)
        {
            CommandEventHandler handler = null;
            handler = (sender, e) =>
            {
                doc.CommandEnded -= handler;
                doc.CommandCancelled -= handler;
                doc.CommandFailed -= handler;

                EraseEntity(doc.Database, cloneId);
            };

            doc.CommandEnded += handler;
            doc.CommandCancelled += handler;
            doc.CommandFailed += handler;
        }

        private static void EraseEntity(Database db, ObjectId id)
        {
            if (id.IsNull || !id.IsValid || id.IsErased) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                ent.Erase();
                tr.Commit();
            }
        }

        private static ObjectId CurrentSpaceId(Database db)
        {
            return db.CurrentSpaceId;
        }
    }
}
