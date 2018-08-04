// 
// Main website for TVRename is http://tvrename.com
// 
// Source code available at https://github.com/TV-Rename/tvrename
// 
// This code is released under GPLv3 https://github.com/TV-Rename/tvrename/blob/master/LICENSE.md
// 

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

//            this->txtCustomShowName->TextChanged += gcnew System::EventHandler(this, &AddEditShow::txtCustomShowName_TextChanged);

namespace TVRename
{
    /// <summary>
    /// Summary for AddEditShow
    ///
    /// WARNING: If you change the name of this class, you will need to change the
    ///          'Resource File Name' property for the managed resource compiler tool
    ///          associated with all .resx files this class depends on.  Otherwise,
    ///          the designers will not be able to interact properly with localized
    ///          resources associated with this form.
    /// </summary>
    public partial class AddEditShow : Form
    {
        private readonly ShowItem selectedShow;
        private readonly TheTvdbCodeFinder codeFinderForm;
        private CustomNameTagsFloatingWindow cntfw;
        private readonly Season sampleSeason;

        public AddEditShow(ShowItem si)
        {
            selectedShow = si;
            sampleSeason = si.GetFirstAvailableSeason();
            InitializeComponent();

            cbTimeZone.BeginUpdate();
            cbTimeZone.Items.Clear();

            lblSeasonWordPreview.Text = TVSettings.Instance.SeasonFolderFormat + "-("+ CustomSeasonName.NameFor(si.GetFirstAvailableSeason(), TVSettings.Instance.SeasonFolderFormat) + ")";
            lblSeasonWordPreview.ForeColor = Color.DarkGray;

            foreach (string s in TimeZone.ZoneNames())
                cbTimeZone.Items.Add(s);

            cbTimeZone.EndUpdate();
            cbTimeZone.Text = si.ShowTimeZone;

            codeFinderForm =
                new TheTvdbCodeFinder(si.TVDBCode != -1 ? si.TVDBCode.ToString() : "") {Dock = DockStyle.Fill};

            pnlCF.SuspendLayout();
            pnlCF.Controls.Add(codeFinderForm);
            pnlCF.ResumeLayout();

            cntfw = null;
            chkCustomShowName.Checked = si.UseCustomShowName;
            if (chkCustomShowName.Checked)
                txtCustomShowName.Text = si.CustomShowName;
            chkCustomShowName_CheckedChanged(null, null);

            cbSequentialMatching.Checked = si.UseSequentialMatch;
            chkShowNextAirdate.Checked = si.ShowNextAirdate;
            chkSpecialsCount.Checked = si.CountSpecials;
            txtBaseFolder.Text = si.AutoAdd_FolderBase;

            cbDoRenaming.Checked = si.DoRename;
            cbDoMissingCheck.Checked = si.DoMissingCheck;
            cbDoMissingCheck_CheckedChanged(null, null);

            switch (si.AutoAdd_Type)
            {
                case ShowItem.AutomaticFolderType.None:
                    chkAutoFolders.Checked = false;
                    break;
                case ShowItem.AutomaticFolderType.BaseOnly:
                    chkAutoFolders.Checked = true;
                    rdoFolderBaseOnly.Checked = true;
                    break;
                case ShowItem.AutomaticFolderType.Custom:
                    chkAutoFolders.Checked = true;
                    rdoFolderCustom.Checked = true;
                    break;
                case ShowItem.AutomaticFolderType.LibraryDefault:
                default:
                    chkAutoFolders.Checked = true;
                    rdoFolderLibraryDefault.Checked = true;
                    break;
            }

            txtSeasonFormat.Text = si.AutoAdd_CustomFolderFormat;

            chkDVDOrder.Checked = si.DVDOrder;
            cbIncludeFuture.Checked = si.ForceCheckFuture;
            cbIncludeNoAirdate.Checked = si.ForceCheckNoAirdate;

            bool first = true;
            si.IgnoreSeasons.Sort();
            foreach (int i in si.IgnoreSeasons)
            {
                if (!first)
                    txtIgnoreSeasons.Text += " ";
                txtIgnoreSeasons.Text += i.ToString();
                first = false;
            }

            foreach (KeyValuePair<int, List<string>> kvp in si.ManualFolderLocations)
            {
                foreach (string s in kvp.Value)
                {
                    ListViewItem lvi = new ListViewItem {Text = kvp.Key.ToString()};
                    lvi.SubItems.Add(s);

                    lvSeasonFolders.Items.Add(lvi);
                }
            }
            lvSeasonFolders.Sort();

            txtSeasonNumber_TextChanged(null, null);
            txtFolder_TextChanged();

            ActiveControl = codeFinderForm; // set initial focus to the code entry/show finder control

            foreach (string aliasName in selectedShow.AliasNames)
            {
                lbShowAlias.Items.Add(aliasName);
            }

            StringBuilder tl = new StringBuilder();

            foreach (string s in CustomEpisodeName.Tags)
            {
                tl.AppendLine(s);
            }
            txtTagList.Text = tl.ToString();

            cbUseCustomSearch.Checked = si.UseCustomSearchURL && !string.IsNullOrWhiteSpace(si.CustomSearchURL);
            txtSearchURL.Text = si.CustomSearchURL ?? "";
            EnableDisableCustomSearch();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (!OkToClose())
            {
                DialogResult = DialogResult.None;
                return;
            }

            SetShow();
            DialogResult = DialogResult.OK;
            Close();
        }

        private bool OkToClose()
        {
            if (!TheTVDB.Instance.HasSeries(codeFinderForm.SelectedCode()))
            {
                DialogResult dr = MessageBox.Show("tvdb code unknown, close anyway?", "TVRename Add/Edit Show",
                                                  MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr == DialogResult.No)
                    return false;
            }

            return true;
        }

        private void SetShow()
        {
            int code = codeFinderForm.SelectedCode();


            selectedShow.CustomShowName = txtCustomShowName.Text;
            selectedShow.UseCustomShowName = chkCustomShowName.Checked;
            selectedShow.ShowTimeZone = cbTimeZone.SelectedItem?.ToString() ?? TimeZone.DefaultTimeZone();
            selectedShow.ShowNextAirdate = chkShowNextAirdate.Checked;
            selectedShow.TVDBCode = code;
            selectedShow.CountSpecials = chkSpecialsCount.Checked;
            selectedShow.DoRename = cbDoRenaming.Checked;
            selectedShow.DoMissingCheck = cbDoMissingCheck.Checked;
            selectedShow.AutoAdd_CustomFolderFormat = txtSeasonFormat.Text;
            selectedShow.AutoAdd_FolderBase = txtBaseFolder.Text;

            if (rdoFolderCustom.Checked)
                selectedShow.AutoAdd_Type = ShowItem.AutomaticFolderType.Custom;
            else if (rdoFolderBaseOnly.Checked)
                selectedShow.AutoAdd_Type = ShowItem.AutomaticFolderType.BaseOnly;
            else if (rdoFolderLibraryDefault.Checked)
                selectedShow.AutoAdd_Type = ShowItem.AutomaticFolderType.LibraryDefault;
            else
                selectedShow.AutoAdd_Type = ShowItem.AutomaticFolderType.None;

            selectedShow.DVDOrder = chkDVDOrder.Checked;
            selectedShow.ForceCheckFuture = cbIncludeFuture.Checked;
            selectedShow.ForceCheckNoAirdate = cbIncludeNoAirdate.Checked;
            selectedShow.UseCustomSearchURL = cbUseCustomSearch.Checked;
            selectedShow.CustomSearchURL = txtSearchURL.Text;

            selectedShow.UseSequentialMatch = cbSequentialMatching.Checked;

            string slist = txtIgnoreSeasons.Text;
            selectedShow.IgnoreSeasons.Clear();
            foreach (Match match in Regex.Matches(slist, "\\b[0-9]+\\b"))
                selectedShow.IgnoreSeasons.Add(int.Parse(match.Value));

            selectedShow.ManualFolderLocations.Clear();
            foreach (ListViewItem lvi in lvSeasonFolders.Items)
            {
                try
                {
                    int seas = int.Parse(lvi.Text);
                    if (!selectedShow.ManualFolderLocations.ContainsKey(seas))
                        selectedShow.ManualFolderLocations.Add(seas, new List<string>());

                    selectedShow.ManualFolderLocations[seas].Add(lvi.SubItems[1].Text);
                }
                catch
                {
                    // ignored
                }
            }

            selectedShow.AliasNames.Clear();
            foreach (string showAlias in lbShowAlias.Items)
            {
                if (!selectedShow.AliasNames.Contains(showAlias))
                {
                    selectedShow.AliasNames.Add(showAlias);
                }
            }
        }

        private void bnCancel_Click(object sender, EventArgs e) => Close();

        private void bnBrowse_Click(object sender, EventArgs e)
        {
            searchFolderBrowser.Title = "Add Folder...";
            searchFolderBrowser.ShowEditbox = true;
            searchFolderBrowser.ShowNewFolderButton = true;
            searchFolderBrowser.StartPosition = FormStartPosition.CenterScreen;

            if (!string.IsNullOrEmpty(txtBaseFolder.Text))
                searchFolderBrowser.SelectedPath = txtBaseFolder.Text;

            if (searchFolderBrowser.ShowDialog(this) == DialogResult.OK)
                txtBaseFolder.Text = searchFolderBrowser.SelectedPath;
        }

        private void cbDoMissingCheck_CheckedChanged(object sender, EventArgs e)
        {
            cbIncludeNoAirdate.Enabled = cbDoMissingCheck.Checked;
            cbIncludeFuture.Enabled = cbDoMissingCheck.Checked;
        }

        private void bnRemove_Click(object sender, EventArgs e)
        {
            if (lvSeasonFolders.SelectedItems.Count > 0)
            {
                foreach (ListViewItem lvi in lvSeasonFolders.SelectedItems)
                    lvSeasonFolders.Items.Remove(lvi);
            }
        }

        private void bnAdd_Click(object sender, EventArgs e)
        {
            ListViewItem lvi = new ListViewItem {Text = txtSeasonNumber.Text};
            lvi.SubItems.Add(txtFolder.Text);

            lvSeasonFolders.Items.Add(lvi);

            txtSeasonNumber.Text = "";
            txtFolder.Text = "";

            lvSeasonFolders.Sort();
        }

        private void bnBrowseFolder_Click(object sender, EventArgs e)
        {
            searchFolderBrowser.Title = "Add Folder...";
            searchFolderBrowser.ShowEditbox = true;
            searchFolderBrowser.ShowNewFolderButton = true;
            searchFolderBrowser.StartPosition = FormStartPosition.CenterScreen;

            if (!string.IsNullOrEmpty(txtFolder.Text))
                searchFolderBrowser.SelectedPath = txtFolder.Text;

            if(string.IsNullOrWhiteSpace(searchFolderBrowser.SelectedPath) && !string.IsNullOrWhiteSpace(txtBaseFolder.Text))
                searchFolderBrowser.SelectedPath = txtBaseFolder.Text;

            if (searchFolderBrowser.ShowDialog(this) == DialogResult.OK)
                txtFolder.Text = searchFolderBrowser.SelectedPath;
        }

        private void txtSeasonNumber_TextChanged(object sender, EventArgs e) => CheckToEnableAddButton();

        private void CheckToEnableAddButton()
        {
            bool isNumber = Regex.Match(txtSeasonNumber.Text, "^[0-9]+$").Success;
            bnAdd.Enabled = isNumber && (!string.IsNullOrEmpty(txtSeasonNumber.Text));
        }

        private void txtFolder_TextChanged()
        {
            bool ok = true;
            if (!string.IsNullOrEmpty(txtFolder.Text))
            {
                try
                {
                    ok = System.IO.Directory.Exists(txtFolder.Text);
                }
                catch
                {
                    // ignored
                }
            }
            txtFolder.BackColor = ok ? SystemColors.Window : Helpers.WarningColor();
        }

        private void chkCustomShowName_CheckedChanged(object sender, EventArgs e)
        {
            txtCustomShowName.Enabled = chkCustomShowName.Checked;
        }

        private void chkAutoFolders_CheckedChanged(object sender, EventArgs e)
        {
            gbAutoFolders.Enabled = chkAutoFolders.Checked;
        }

        private void bnAddAlias_Click(object sender, EventArgs e)
        {
            string aliasName = tbShowAlias.Text;

            if (string.IsNullOrEmpty(aliasName)) return;

            if (lbShowAlias.FindStringExact(aliasName) == -1)
            {
                lbShowAlias.Items.Add(aliasName);
            }
            tbShowAlias.Text = "";
        }

        private void bnRemoveAlias_Click(object sender, EventArgs e)
        {
            if (lbShowAlias.SelectedItems.Count > 0)
            {
                foreach (int i in lbShowAlias.SelectedIndices)
                {
                    lbShowAlias.Items.RemoveAt(i);
                }
            }
        }

        private void tbShowAlias_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                bnAddAlias_Click(null, null);
        }

        private void cbUseCustomSearch_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableCustomSearch();
        }

        private void EnableDisableCustomSearch()
        {
            bool en = cbUseCustomSearch.Checked;

            lbSearchURL.Enabled = en;
            txtSearchURL.Enabled = en;
            lbTags.Enabled = en;
            txtTagList.Enabled = en;
        }

        private void tbShowAlias_TextChanged(object sender, EventArgs e)
        {
          bnAddAlias.Enabled = tbShowAlias.Text.Length > 0;
        }

        private void lbShowAlias_SelectedIndexChanged(object sender, EventArgs e)
        {
            bnRemoveAlias.Enabled = lbShowAlias.SelectedItems.Count > 0;
        }

        private void bnTags_Click(object sender, EventArgs e)
        {
                cntfw = new CustomNameTagsFloatingWindow(sampleSeason);
                cntfw.Show(this);
                Focus();
        }

        private void txtFolder_TextChanged(object sender, EventArgs e) => CheckToEnableAddButton();

        private void lvSeasonFolders_SelectedIndexChanged(object sender, EventArgs e)
        {
            bnRemove.Enabled = lvSeasonFolders.SelectedItems.Count > 0;
        }
    }
}