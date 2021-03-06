﻿using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Remotion.Linq.Clauses.ResultOperators;
using SoraBot_v2.Data;
using SoraBot_v2.Services;

namespace SoraBot_v2.Module
{
    public class StarboardModule : ModuleBase<SocketCommandContext>
    {
        public StarboardModule()
        {
        }

        private bool CheckPerms(SocketCommandContext context)
        {
            var invoker = (SocketGuildUser)context.User;
            if (!invoker.GuildPermissions.Has(GuildPermission.Administrator) &&
                !invoker.GuildPermissions.Has(GuildPermission.ManageChannels) && !Utility.IsSoraAdmin(invoker))
            {
                return false;
            }
            return true;
        }

        [Command("starchannel"), Alias("star"), Summary("Sets current or specified channel as starboard channel!")]
        public async Task AddStarChannel([Summary("Channel for starboard. Leave blank to use current channel")] ISocketMessageChannel channel = null)
        {
            var starChannel = channel ?? Context.Channel;
            if (CheckPerms(Context) == false)
            {
                await ReplyAsync("", embed: Utility.ResultFeedback(Utility.RedFailiureEmbed,
                    Utility.SuccessLevelEmoji[2], "You need Administrator or Mange Channels permission to set the starboard channel!"));
                return;
            }

            using (var _soraContext = new SoraContext())
            {
                var guildDb = Utility.GetOrCreateGuild(Context.Guild.Id, _soraContext);
                guildDb.StarChannelId = starChannel.Id;
                await _soraContext.SaveChangesAsync();
                await ReplyAsync("", embed: Utility.ResultFeedback(Utility.GreenSuccessEmbed,
                    Utility.SuccessLevelEmoji[0], "Successfully set starboard channel").WithDescription($"<#{starChannel.Id}>"));
            }
        }

        [Command("starremove"), Alias("rmstar", "starrm"),
         Summary("Removes the Starboard channel if not already removed")]
        public async Task RemoveStarChannel()
        {
            if (CheckPerms(Context) == false)
            {
                await ReplyAsync("", embed: Utility.ResultFeedback(Utility.RedFailiureEmbed,
                    Utility.SuccessLevelEmoji[2], $"You need Administrator or Mange Channels permission or the {Utility.SORA_ADMIN_ROLE_NAME} role to remove the starboard channel"));
                return;
            }

            using (var _soraContext = new SoraContext())
            {
                var guildDb = Utility.GetOrCreateGuild(Context.Guild.Id, _soraContext);
                if (guildDb.StarChannelId == 0)
                {
                    await ReplyAsync("", embed: Utility.ResultFeedback(Utility.RedFailiureEmbed,
                        Utility.SuccessLevelEmoji[2], "No Starboard channel set!"));
                    return;
                }
                guildDb.StarChannelId = 0;
                await _soraContext.SaveChangesAsync();
                await ReplyAsync("", embed: Utility.ResultFeedback(Utility.GreenSuccessEmbed,
                    Utility.SuccessLevelEmoji[0], $"Successfully removed the starboard channel"));
            }
        }

        [Command("starlimit"), Alias("starminimum"),
         Summary("Specify the lowest amount of stars needed to a message until it gets posted to the starboard")]
        public async Task SetStarLimit([Summary("WHOLE number of amount needed")] int amount)
        {
            if (CheckPerms(Context) == false)
            {
                await ReplyAsync("", embed: Utility.ResultFeedback(Utility.RedFailiureEmbed,
                    Utility.SuccessLevelEmoji[2], "You need Administrator or Mange Channels permission to set the starlimit!"));
                return;
            }
            if (amount > 100)
                amount = 100;
            if (amount < 1)
                amount = 1;

            using (var _soraContext = new SoraContext())
            {
                var guildDb = Utility.GetOrCreateGuild(Context.Guild.Id, _soraContext);
                guildDb.StarMinimum = amount;
                await _soraContext.SaveChangesAsync();
                await ReplyAsync("", embed: Utility.ResultFeedback(Utility.GreenSuccessEmbed,
                    Utility.SuccessLevelEmoji[0], $"Successfully changed minimum Star requirement to {amount}"));
            }
        }
    }
}