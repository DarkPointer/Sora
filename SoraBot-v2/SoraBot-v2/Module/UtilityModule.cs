﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SoraBot_v2.Data;
using SoraBot_v2.Services;

namespace SoraBot_v2.Module
{
    public class UtilityModule : ModuleBase<SocketCommandContext>
    {
        public UtilityModule()
        {
        }

        [Command("gc")]
        [RequireOwner]
        public async Task ForceGC()
        {
            GC.Collect();
        }

        [Command("reloadconfig"), Alias("reconf")]
        [RequireOwner]
        public async Task ReloadConfig()
        {
            ConfigService.LoadConfig();
            await ReplyAsync("", embed: Utility.ResultFeedback(Utility.GreenSuccessEmbed, Utility.SuccessLevelEmoji[0], "Successfully reloaded config.json"));
        } 

        [Command("oginfo")]
        [RequireOwner]
        public async Task Guildinfo(ulong id)
        {
            var guild = Context.Client.GetGuild(id);
            if (guild == null)
            {
                await ReplyAsync("Guild not found");
                return;
            }
            await ReplyAsync($"```\n" +
                             $"Guild Name: {guild.Name}\n" +
                             $"User Count: {guild.Users.Count}\n" +
                             $"Owner: {Utility.GiveUsernameDiscrimComb(guild.Owner)}\n" +
                             $"```");
        }

        [Command("leaveguild")]
        [RequireOwner]
        public async Task LeaveGuild(ulong id)
        {
            var guild = Context.Client.GetGuild(id);
            if (guild == null)
            {
                await ReplyAsync("Guild not found");
                return;
            }
            await guild.LeaveAsync();
            await ReplyAsync("Left guild.");
        }

        [Command("createAdmin"), Alias("ca", "createsoraadmin", "csa"), Summary("Creates the Admin Role for Sora!")]
        public async Task CreateSoraAdminRole()
        {
            var invoker = Context.User as SocketGuildUser;
            if (!invoker.GuildPermissions.Has(GuildPermission.Administrator))
            {
                await ReplyAsync("", embed: Utility.ResultFeedback(Utility.RedFailiureEmbed, Utility.SuccessLevelEmoji[2], $"You need Administrator permissions to create the {Utility.SORA_ADMIN_ROLE_NAME}"));
                return;
            }
            if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GuildPermissions.Has(GuildPermission.ManageRoles))
            {
                await ReplyAsync("", embed: Utility.ResultFeedback(Utility.RedFailiureEmbed, Utility.SuccessLevelEmoji[2], $"Sora does not have Manage Role Permissions!"));
                return;
            }

            //Check if already exists
            if (Utility.CheckIfSoraAdminExists(Context.Guild))
            {
                await ReplyAsync("", embed: Utility.ResultFeedback(Utility.RedFailiureEmbed, Utility.SuccessLevelEmoji[2], $"The {Utility.SORA_ADMIN_ROLE_NAME} Role already exists!"));
                return;
            }
            //Create role
            await Context.Guild.CreateRoleAsync(Utility.SORA_ADMIN_ROLE_NAME, GuildPermissions.None);
            await ReplyAsync("",
                embed: Utility.ResultFeedback(Utility.GreenSuccessEmbed, Utility.SuccessLevelEmoji[0],
                    $"Successfully created {Utility.SORA_ADMIN_ROLE_NAME} Role!"));
        }

        [Command("restrictdj"), Alias("forcedj", "djonly"), Summary("Restricts most of the music commands to members carrying the Sora-DJ role")]
        public async Task RestrictDj()
        {
            var invoker = (SocketGuildUser)Context.User;
            if (!invoker.GuildPermissions.Has(GuildPermission.Administrator) && !Utility.IsSoraAdmin(invoker))
            {
                await ReplyAsync("", embed: Utility.ResultFeedback(Utility.RedFailiureEmbed, Utility.SuccessLevelEmoji[2], $"You need Administrator permissions or the {Utility.SORA_ADMIN_ROLE_NAME} role to restrict music usage!"));
                return;
            }

            using (var _soraContext = new SoraContext())
            {
                var guildDb = Utility.GetOrCreateGuild(Context.Guild.Id, _soraContext);
                if (guildDb.IsDjRestricted)
                {
                    //MAKE IT UNRESTRICTED
                    guildDb.IsDjRestricted = false;
                    await _soraContext.SaveChangesAsync();
                    await ReplyAsync("", embed: Utility.ResultFeedback(Utility.GreenSuccessEmbed, Utility.SuccessLevelEmoji[0], $"Successfully unrestricted all music commands!"));
                    await ReplyAsync("", embed: Utility.ResultFeedback(Utility.GreenSuccessEmbed, Utility.SuccessLevelEmoji[0], $"Successfully unrestricted all music commands!"));
                    return;
                }
                //Restrict them
                bool created = false;
                if (Context.Guild.Roles.All(x => x.Name != Utility.SORA_DJ_ROLE_NAME))
                {
                    //DJ Role doesn't exist
                    if (!Context.Guild.GetUser(Context.Client.CurrentUser.Id).GuildPermissions.Has(GuildPermission.ManageRoles))
                    {
                        //CANT CREATE ONE EITHER
                        await ReplyAsync("", embed: Utility.ResultFeedback(Utility.RedFailiureEmbed, Utility.SuccessLevelEmoji[2],
                            $"The {Utility.SORA_DJ_ROLE_NAME} does not exist and Sora doesn't have manage role permission!").WithDescription(
                            $"Either create the {Utility.SORA_DJ_ROLE_NAME} yourself (case sensitive) or give sora Manage Role permission and let him create it!"));
                        return;
                    }
                    //Create the Role
                    try
                    {
                        await Context.Guild.CreateRoleAsync(Utility.SORA_DJ_ROLE_NAME, GuildPermissions.None);
                    }
                    catch (Exception)
                    {
                        await ReplyAsync("", embed: Utility.ResultFeedback(Utility.RedFailiureEmbed, Utility.SuccessLevelEmoji[2], $"Couldn't Create role for some reason. Check perms!"));
                        return;
                    }
                    created = true;
                }

                guildDb.IsDjRestricted = true;
                await _soraContext.SaveChangesAsync();
                await ReplyAsync("", embed: Utility.ResultFeedback(Utility.GreenSuccessEmbed, Utility.SuccessLevelEmoji[0],
                    $"Successfully restricted all music commands{(created ? $" and created {Utility.SORA_DJ_ROLE_NAME} role!" : "!")}"));
            }
        }
    }
}