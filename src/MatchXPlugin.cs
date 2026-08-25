using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(MatchX.MatchXPlugin))]

namespace MatchX
{
    public class MatchXPlugin : IExtensionApplication
    {
        public void Initialize()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage("\nMatchX loaded. Run MX to begin.");
        }

        public void Terminate()
        {
        }
    }
}
