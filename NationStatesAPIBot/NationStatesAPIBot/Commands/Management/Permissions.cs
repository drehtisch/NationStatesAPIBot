﻿using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using NationStatesAPIBot.Entities;
using NationStatesAPIBot.Managers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Commands.Management
{

    public class PermissionCommands : ModuleBase<SocketCommandContext>
    {
        [Command("checkUser"), Summary("Returns Permission of specified User")]
        public async Task DoCheckUser(string id)
        {
            if (Context.User.Id.ToString() == ActionManager.BotAdminDiscordUserId)
            {
                using (var dbContext = new BotDbContext())
                {
                    var channel = await Context.User.GetOrCreateDMChannelAsync();
                    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.DiscordUserId == Context.User.Id.ToString());
                    if (user == null)
                    {
                        await channel.SendMessageAsync("User not found");
                    }
                    else
                    {
                        string permissions = "Permissions: ";
                        var perms = PermissionManager.GetAllPermissionsToAUser(id, dbContext);
                        if (perms.Count() > 0)
                        {
                            foreach (var perm in perms)
                            {
                                permissions += perm.Name + ";";
                            }
                            await channel.SendMessageAsync(permissions);
                        }
                        else
                        {
                            await channel.SendMessageAsync("No permissions found.");
                        }
                    }
                }
            }
            else
            {
                await ReplyAsync(ActionManager.PERMISSION_DENIED_RESPONSE);
            }
        }

        [Command("checkPerm"), Summary("Returns all Users who have specified permission")]
        public async Task DoCheckPerm(long id)
        {
            if (Context.User.Id.ToString() == ActionManager.BotAdminDiscordUserId)
            {
                using (var dbContext = new BotDbContext())
                {
                    var perm = await dbContext.Permissions.FirstOrDefaultAsync(p => p.Id == id);
                    var channel = await Context.User.GetOrCreateDMChannelAsync();
                    if (perm == null)
                    {
                        await channel.SendMessageAsync("Permission not found");
                    }
                    else
                    {
                        string permissions = "Users: ";
                        var users = dbContext.Permissions.Where(p => p.Id == id).SelectMany(u => u.UserPermissions).Select(p => p.User);
                        if (users.Count() > 0)
                        {
                            foreach (var user in users)
                            {
                                permissions += user.DiscordUserId + ";";
                            }
                            await channel.SendMessageAsync(permissions);
                        }
                        else
                        {
                            await channel.SendMessageAsync("No users found.");
                        }
                    }
                }
            }
            else
            {
                await ReplyAsync(ActionManager.PERMISSION_DENIED_RESPONSE);
            }
        }

        [Command("addUser"), Summary("Adds a User to the database")]
        public async Task DoAddUser(string id)
        {
            if (Context.User.Id.ToString() == ActionManager.BotAdminDiscordUserId)
            {
                using (var dbContext = new BotDbContext())
                {
                    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.DiscordUserId == id);
                    if (user == null)
                    {
                        await ActionManager.AddUserToDbAsync(id);
                        var channel = await Context.User.GetOrCreateDMChannelAsync();
                        await channel.SendMessageAsync("User not found. -> User added");
                    }
                    else
                    {
                        var channel = await Context.User.GetOrCreateDMChannelAsync();
                        await channel.SendMessageAsync("Already exists.");
                    }
                }
            }
            else
            {
                await ReplyAsync(ActionManager.PERMISSION_DENIED_RESPONSE);
            }
        }

        [Command("grantPermission"), Summary("Adds a User to the database")]
        public async Task DoGrantPermission(string id, int permissionId)
        {
            if (Context.IsPrivate)
            {
                if (PermissionManager.IsAllowed(Types.PermissionType.ManagePermissions, Context.User))
                {
                    using (var dbContext = new BotDbContext())
                    {
                        var channel = await Context.User.GetOrCreateDMChannelAsync();
                        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.DiscordUserId == id);
                        var permission = await dbContext.Permissions.FirstOrDefaultAsync(p => p.Id == permissionId);
                        if (permission != null)
                        {
                            if (user == null)
                            {
                                user = new User() { DiscordUserId = Context.User.Id.ToString() };
                                await dbContext.Users.AddAsync(user);

                                await channel.SendMessageAsync("User not found. -> User added");
                            }
                            if (user.UserPermissions == null)
                            {
                                user.UserPermissions = new System.Collections.Generic.List<UserPermissions>();
                            }
                            user.UserPermissions.Add(new UserPermissions() { PermissionId = permissionId, UserId = user.Id, User = user, Permission = permission });
                            dbContext.Update(user);
                            await dbContext.SaveChangesAsync();
                            await channel.SendMessageAsync("Granted Permission");

                        }
                        else
                        {
                            await channel.SendMessageAsync("Permission not found.");
                        }
                    }
                }
                else
                {
                    await ReplyAsync(ActionManager.PERMISSION_DENIED_RESPONSE);
                }
            }
            else
            {
                var content = Context.Message.Content;
                await Context.Message.DeleteAsync();
                var channel = await Context.User.GetOrCreateDMChannelAsync();
                await channel.SendMessageAsync($"This command is confidential. " +
                    $"So it is only accepted in private channels. " +
                    $"I removed your command from the other channel. " +
                    $"Please try again here. " +
                    $"Sorry for the inconvenience. " + Environment.NewLine +
                    $"Your command was: {content}");
            }
        }
    }
}