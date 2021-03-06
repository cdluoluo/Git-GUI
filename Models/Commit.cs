﻿using System;
using System.Collections.Generic;
using GG.Libraries;
using System.Linq;
using System.Collections.ObjectModel;

namespace GG.Models
{
    public class Commit
    {
        public string       AuthorEmail      { get; set; }
        public string       AuthorName       { get; set; }
        public DateTime     Date             { get; set; }
        public string       Description      { get; set; }
        public string       ShortDescription { get; set; }
        public List<string> DisplayTags      { get; set; }
        public ObservableCollection<Tag> Tags { get; set; }
        public string       Hash             { get; set; }
        public List<Branch> Branches         { get; set; }
        public List<Branch> BranchesAround   { get; set; }
        public List<string> ParentHashes     { get; set; }
        public int          ParentCount      { get; set; }
        public List<Commit> Parents          { get; set; }
        public int          VisualPosition   { get; set; }
        public bool         IsHead           { get; set; }
        public List<Commit> Children         { get; set; }

        /// <summary>
        /// Returns the ObjectId of LibGit2Sharp.
        /// </summary>
        public LibGit2Sharp.ObjectId ObjectId { get; set; }

        /// <summary>
        /// Returns the date of this changeset in relative format.
        /// </summary>
        public string FormattedDate
        {
            get
            {
                return DateUtil.GetRelativeDate(Date);
            }
        }

        /// <summary>
        /// Returns a 7 character wide hash string.
        /// </summary>
        public string HashShort
        {
            get
            {
                return Hash.Substring(0, 7);
            }
        }

        /// <summary>
        /// Returns the author in format "name &lt;email&gt;".
        /// </summary>
        public string Author
        {
            get
            {
                return AuthorName + " <" + AuthorEmail + ">";
            }
        }

        /// <summary>
        /// Returns whether this is a merge commit.
        /// </summary>
        /// <returns></returns>
        public bool IsMergeCommit()
        {
            return ParentCount > 1;
        }

        /// <summary>
        /// Creates a new commit object from the given parameters.
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="commit"></param>
        /// <param name="tags"> </param>
        /// <returns></returns>
        public static Commit Create(LibGit2Sharp.Repository repo,
                                    LibGit2Sharp.Commit commit,
                                    ObservableCollection<Tag> tags)
        {
            var c = new Commit();

            // Process Tags (Git tags to display next to the commit description).
            var commitTags = new ObservableCollection<Tag>();
            foreach (var tag in tags)
            {
                if (tag.TargetSha == commit.Sha)
                {
                    commitTags.Add(tag);
                    tag.Target = c;
                }
            }

            // Process display tags.
            var displayTags = new List<string>();
            if (repo.Head.Tip == commit)
                displayTags.Add("HEAD");

            // Process ParentHashes.
            var parentHashes = new List<string>();
            foreach (var parentCommit in commit.Parents)
            {
                parentHashes.Add(parentCommit.Sha);
            }

            // Set properties.
            c.AuthorEmail  = commit.Author.Email;
            c.AuthorName   = commit.Author.Name;
            c.Date         = commit.Author.When.DateTime;
            c.Description  = commit.Message;
            c.ShortDescription = commit.Message.Right(72).RemoveLineBreaks();
            c.DisplayTags  = displayTags;
            c.Branches     = new List<Branch>();
            c.Tags         = commitTags;
            c.Hash         = commit.Sha;
            c.ParentHashes = parentHashes;
            c.ParentCount  = commit.ParentsCount;
            c.Parents      = new List<Commit>();
            c.ObjectId     = commit.Id;
            c.VisualPosition = -1; // -1 means it's not yet calculated.
            c.Children     = new List<Commit>();

            return c;
        }

        /// <summary>
        /// Post-processes the commit. This means that we set up the parent object relationship.
        /// </summary>
        /// <param name="Commits"></param>
        public void PostProcess(ObservableCollection<Commit> commits, ObservableCollection<Branch> branches)
        {
            // Set Parents.
            if (ParentCount > 0)
            {
                foreach (string hash in ParentHashes)
                {
                    Commit parentCommit = commits.Where(c => c.Hash == hash).FirstOrDefault();

                    if (parentCommit != null)
                    {
                        Parents.Add(parentCommit);
                        parentCommit.Children.Add(this);
                    }
                }
            }

            // Set BranchesAround.
            BranchesAround = RepoUtil.GetBranchesAroundCommit(this, branches);
        }
    }
}