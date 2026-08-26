using System.Collections.Generic;
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
        private static Database? _sourceDatabase = null;

        [CommandMethod("MX")]
        public static void Mx()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            if (_sourceId.IsNull)
            {
                PromptEntityOptions peo = new PromptEntityOptions("\nMatchX - select source entity: ");
                PromptEntityResult per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK) return;

                _sourceId = per.ObjectId;
                _sourceDatabase = db;

                ed.WriteMessage("\nMatchX - source captured. Run MX again and select destination entities. Run MXRESET to pick a new source.");
                return;
            }

            if (!_sourceId.IsValid || _sourceId.IsErased)
            {
                ed.WriteMessage("\nMatchX: source entity no longer exists — run MX to pick a new source.");
                _sourceId = ObjectId.Null;
                _sourceDatabase = null;
                return;
            }

            if (db != _sourceDatabase)
            {
                ed.WriteMessage("\nMatchX: source was captured in a different document — run MX to pick a new source.");
                _sourceId = ObjectId.Null;
                _sourceDatabase = null;
                return;
            }

            if (_sourceId.IsNull)
            {
                ed.WriteMessage("\nMatchX: no source captured — run MX to select a source first.");
                return;
            }

            (int count, int skippedLockedLayer) = PaintDestinations(ed, db, _sourceId);
            ed.WriteMessage($"\nMatchX: {count} entities updated");
            if (skippedLockedLayer > 0)
            {
                ed.WriteMessage($"\nMatchX: {skippedLockedLayer} entity(ies) skipped — locked layer.");
            }
        }

        [CommandMethod("MXRESET")]
        public static void MxReset()
        {
            _sourceId = ObjectId.Null;
            _sourceDatabase = null;

            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage("\nMatchX - source cleared.");
        }

        private static (int count, int skippedLockedLayer) PaintDestinations(Editor ed, Database db, ObjectId sourceId)
        {
            int count = 0;
            int skippedLockedLayer = 0;
            HashSet<ObjectId> updatedIds = new HashSet<ObjectId>();

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity source = (Entity)tr.GetObject(sourceId, OpenMode.ForRead);

                    while (true)
                    {
                        PromptSelectionOptions pso = new PromptSelectionOptions
                        {
                            MessageForAdding = "\nMatchX - select destination entities (click or window), or press Enter to finish: "
                        };
                        PromptSelectionResult psr = ed.GetSelection(pso);

                        if (psr.Status == PromptStatus.Cancel || psr.Status == PromptStatus.Error) break;
                        if (psr.Status != PromptStatus.OK) continue;

                        foreach (SelectedObject selectedObject in psr.Value)
                        {
                            ObjectId destinationId = selectedObject.ObjectId;

                            if (destinationId == sourceId) continue;
                            if (updatedIds.Contains(destinationId)) continue;

                            Entity destination = (Entity)tr.GetObject(destinationId, OpenMode.ForRead);

                            LayerTableRecord layer = (LayerTableRecord)tr.GetObject(destination.LayerId, OpenMode.ForRead);
                            if (layer.IsLocked)
                            {
                                skippedLockedLayer++;
                                continue;
                            }

                            destination.UpgradeOpen();

                            destination.Color = source.Color;
                            destination.Layer = source.Layer;
                            destination.Linetype = source.Linetype;
                            destination.LinetypeScale = source.LinetypeScale;
                            destination.LineWeight = source.LineWeight;
                            try { destination.PlotStyleName = source.PlotStyleName; } catch { /* CTB mode — skip */ }
                            destination.Transparency = source.Transparency;

                            CopyThickness(source, destination);
                            CopyTypeSpecificProperties(source, destination);

                            count++;
                            updatedIds.Add(destinationId);

                            Entity destinationForHighlight = (Entity)tr.GetObject(destinationId, OpenMode.ForRead);
                            destinationForHighlight.Highlight();

                            ed.WriteMessage($"\nMatchX: {count} entities updated");
                        }
                    }

                    tr.Commit();
                }
            }
            finally
            {
                if (updatedIds.Count > 0)
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        foreach (ObjectId id in updatedIds)
                        {
                            if (!id.IsValid || id.IsErased) continue;
                            Entity entity = (Entity)tr.GetObject(id, OpenMode.ForRead);
                            entity.Unhighlight();
                        }
                        tr.Commit();
                    }
                }
            }

            return (count, skippedLockedLayer);
        }

        private static void CopyTypeSpecificProperties(Entity source, Entity destination)
        {
            switch (destination)
            {
                case DBText destText when source is DBText srcText:
                    destText.TextStyleId = srcText.TextStyleId;
                    break;

                case MText destMText when source is MText srcMText:
                    destMText.TextStyleId = srcMText.TextStyleId;
                    break;

                case Dimension destDim when source is Dimension srcDim:
                    destDim.DimensionStyleName = srcDim.DimensionStyleName;
                    break;

                case Hatch destHatch when source is Hatch srcHatch:
                    destHatch.SetHatchPattern(srcHatch.PatternType, srcHatch.PatternName);
                    destHatch.PatternScale = srcHatch.PatternScale;
                    destHatch.PatternAngle = srcHatch.PatternAngle;
                    destHatch.HatchStyle = srcHatch.HatchStyle;
                    break;

                case Polyline destPoly when source is Polyline srcPoly:
                    destPoly.ConstantWidth = srcPoly.ConstantWidth;
                    break;

                case Polyline2d destPoly2d when source is Polyline2d srcPoly2d:
                    destPoly2d.ConstantWidth = srcPoly2d.ConstantWidth;
                    break;

                case MLeader destMLeader when source is MLeader srcMLeader:
                    destMLeader.MLeaderStyle = srcMLeader.MLeaderStyle;
                    break;
            }
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
