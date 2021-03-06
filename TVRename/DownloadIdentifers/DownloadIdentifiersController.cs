// 
// Main website for TVRename is http://tvrename.com
// 
// Source code available at https://github.com/TV-Rename/tvrename
// 
// Copyright (c) TV Rename. This code is released under GPLv3 https://github.com/TV-Rename/tvrename/blob/master/LICENSE.md
// 

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;

namespace TVRename
{
    internal class DownloadIdentifiersController
    {
        private readonly List<DownloadIdentifier> identifiers;
        
        public DownloadIdentifiersController() {
            identifiers = new List<DownloadIdentifier>
            {
                new DownloadFolderJpg(),
                new DownloadEpisodeJpg(),
                new DownloadFanartJpg(),
                new DownloadMede8erMetaData(),
                new DownloadpyTivoMetaData(),
                new DownloadWdtvMetaData(),
                new DownloadSeriesJpg(),
                new DownloadKodiMetaData(),
                new DownloadKodiImages(),
                new IncorrectFileDates(),
            };
        }

        public void NotifyComplete(FileInfo file)
        {
            foreach (DownloadIdentifier di in identifiers)
            {
                di.NotifyComplete(file);
            }
        }

        [NotNull]
        public ItemList ProcessShow(ShowItem? si)
        {
            ItemList theActionList = new ItemList();
            if (si is null)
            {
                return theActionList;
            }

            foreach (DownloadIdentifier di in identifiers)
            {
                theActionList.Add(di.ProcessShow(si));
            }
            return theActionList;
        }

        [NotNull]
        public ItemList ProcessSeason(ShowItem? si, string folder, int snum)
        {
            ItemList theActionList = new ItemList();
            if (si is null)
            {
                return theActionList;
            }

            foreach (DownloadIdentifier di in identifiers)
            {
                theActionList.Add(di.ProcessSeason (si,folder,snum));
            }
            return theActionList;
        }

        public ItemList? ProcessEpisode(ProcessedEpisode? episode, FileInfo filo)
        {
            if (episode is null)
            {
                return null;
            }

            ItemList theActionList = new ItemList();
            foreach (DownloadIdentifier di in identifiers)
            {
                theActionList.Add(di.ProcessEpisode(episode,filo));
            }
            return theActionList;
        }

        public  void Reset() {
            foreach (DownloadIdentifier di in identifiers)
            {
                di.Reset();
            }
        }

        [NotNull]
        public ItemList ForceUpdateShow(DownloadIdentifier.DownloadType dt, ShowItem? si)
        {
            ItemList theActionList = new ItemList();
            if (si is null)
            {
                return theActionList;
            }

            foreach (DownloadIdentifier di in identifiers)
            {
                if (dt == di.GetDownloadType())
                {
                    theActionList.Add(di.ProcessShow(si,true));
                }
            }
            return theActionList;
        }

        [NotNull]
        public ItemList ForceUpdateSeason(DownloadIdentifier.DownloadType dt, ShowItem? si, string folder, int snum)
        {
            ItemList theActionList = new ItemList();
            if (si is null)
            {
                return theActionList;
            }

            foreach (DownloadIdentifier di in identifiers)
            {
                if (dt == di.GetDownloadType())
                {
                    theActionList.Add(di.ProcessSeason(si, folder,snum, true));
                }
            }
            return theActionList;
        }

        [NotNull]
        public ItemList ForceUpdateEpisode(DownloadIdentifier.DownloadType dt, ProcessedEpisode episode, FileInfo filo)
        {
            ItemList theActionList = new ItemList();
            foreach (DownloadIdentifier di in identifiers.Where(di => dt == di.GetDownloadType()))
            {
                theActionList.Add(di.ProcessEpisode(episode, filo, true));
            }
            return theActionList;
        }
    }
}
