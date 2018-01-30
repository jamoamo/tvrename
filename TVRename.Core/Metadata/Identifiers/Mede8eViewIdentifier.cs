using Alphaleonis.Win32.Filesystem;
using TVRename.Core.Actions;
using TVRename.Core.Models;

namespace TVRename.Core.Metadata.Identifiers
{
    // ReSharper disable once InconsistentNaming
    public class Mede8erViewIdentifier : TextIdentifier
    {
        public override string TextFormat => "Mede8er View";

        public override TargetTypes SupportedTypes => TargetTypes.Show | TargetTypes.Season;

        protected override IAction ProcessShow(ProcessedShow show, FileInfo file, bool force = false)
        {
            if (!force && file.Exists && show.LastUpdated <= file.LastWriteTime) return null;

            return new Mede8erViewAction(file, false);
        }

        protected override IAction ProcessSeason(ProcessedShow show, ProcessedSeason season, FileInfo file, bool force = false)
        {
            if (!force && file.Exists && show.LastUpdated <= file.LastWriteTime) return null;

            return new Mede8erViewAction(file, true);
        }
    }
}