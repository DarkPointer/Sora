﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SoraBot_v2.Data.Entities.SubEntities;

namespace SoraBot_v2.Data.Entities
{
    public class Guild
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong GuildId { get; set; }
        
        public string Prefix { get; set; }
        public bool RestrictTags { get; set; }
        public bool IsDjRestricted { get; set; }
        public ulong StarChannelId { get; set; }
        public int StarMinimum { get; set; }
        public bool HasDefaultRole { get; set; }
        public ulong DefaultRoleId { get; set; }
        public ulong WelcomeChannelId { get; set; }
        public ulong LeaveChannelId { get; set; }
        public string WelcomeMessage { get; set; }
        public string LeaveMessage { get; set; }
        public bool EmbedWelcome { get; set; }
        public bool EmbedLeave { get; set; }
        public ulong PunishLogsId { get; set; }

        public string LevelUpMessage { get; set; }
        public bool EnabledLvlUpMessage { get; set; }
        public bool SendLvlDm { get; set; }
        
        public virtual List<Tags> Tags { get; set; }
        public virtual List<StarMessage> StarMessages { get; set; }
        public virtual List<Role> SelfAssignableRoles { get; set; }
        public virtual List<ModCase> Cases { get; set; }
        public virtual List<GuildUser> Users { get; set; }
        public virtual List<GuildLevelRole> LevelRoles { get; set; }
    }
}