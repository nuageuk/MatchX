using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace MatchX
{
    public class MxCommand
    {
        private static ObjectId _sourceId = ObjectId.Null;

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

                ed.WriteMessage("\nMatchX - source captured. Run MX again and select destination entities. Run MXRESET to pick a new source.");
                return;
            }

            PromptSelectionOptions pso = new PromptSelectionOptions
            {
                MessageForAdding = "\nMatchX - select destination entities: "
            };
            PromptSelectionResult psr = ed.GetSelection(pso);
            if (psr.Status != PromptStatus.OK) return;

            ObjectId[] destinationIds = psr.Value.GetObjectIds();
            if (destinationIds.Length == 0) return;

            int count = ApplyProperties(db, _sourceId, destinationIds);
            ed.WriteMessage($"\nMatchX - properties applied to {count} entity(ies).");
        }

        [CommandMethod("MXRESET")]
        public static void MxReset()
        {
            _sourceId = ObjectId.Null;

            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage("\nMatchX - source cleared.");
        }

        private static int ApplyProperties(Database db, ObjectId sourceId, ObjectId[] destinationIds)
        {
            int count = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity source = (Entity)tr.GetObject(sourceId, OpenMode.ForRead);

                foreach (ObjectId destinationId in destinationIds)
                {
                    if (destinationId == sourceId) continue;

                    Entity destination = (Entity)tr.GetObject(destinationId, OpenMode.ForWrite);

                    destination.Color = source.Color;
                    destination.Layer = source.Layer;
                    destination.Linetype = source.Linetype;
                    destination.LinetypeScale = source.LinetypeScale;
                    destination.LineWeight = source.LineWeight;
                    destination.PlotStyleName = source.PlotStyleName;
                    destination.Transparency = source.Transparency;

                    CopyThickness(source, destination);

                    count++;
                }

                tr.Commit();
            }

            return count;
        }

        private static void CopyThickness(Entity source, Entity destination)
        {
            PropertyInfo sourceProperty = source.GetType().GetProperty("Thickness");
            PropertyInfo destinationProperty = destination.GetType().GetProperty("Thickness");

            if (sourceProperty == null || destinationProperty == null || !destinationProperty.CanWrite) return;

            destinationProperty.SetValue(destination, sourceProperty.GetValue(source, null), null);
        }
    }
}
