// 
// Main website for TVRename is http://tvrename.com
// 
// Source code available at https://github.com/TV-Rename/tvrename
// 
// Copyright (c) TV Rename. This code is released under GPLv3 https://github.com/TV-Rename/tvrename/blob/master/LICENSE.md
// 

using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace TVRename
{
    public class PreviouslySeenEpisodes : List<int>
    {
        public PreviouslySeenEpisodes()
        {
        }

        public PreviouslySeenEpisodes(XElement? xml)
        {
            if (xml is null)
            {
                return;
            }

            foreach (XElement n in xml.Descendants("Episode"))
            {
                EnsureAdded(XmlConvert.ToInt32(n.Value));
            }
        }

        private void EnsureAdded(int epId)
        {
            if (!Contains(epId) && epId>0)
            {
                Add(epId);
            }
        }

        public void EnsureAdded([NotNull] ProcessedEpisode episode) => EnsureAdded(episode.EpisodeId);

        public bool Includes([NotNull] Item item) => item.Episode != null && item.Episode.EpisodeId >0 && Contains(item.Episode.EpisodeId);

        public bool Includes(ProcessedEpisode? episode) => episode != null && episode.EpisodeId > 0 && Contains(episode.EpisodeId);
    }
}
