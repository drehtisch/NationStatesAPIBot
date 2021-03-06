﻿using Discord.Commands;
using NationStatesAPIBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace NationStatesAPIBot.Commands.Management
{
    public class Codes : ModuleBase<SocketCommandContext>
    {
        [Command("ovc"), Summary("Returns an Ownership Verification Code")]
        public async Task DoGenerateOVCAsync(string nationName)
        {
            var permManager = Program.ServiceProvider.GetService<IPermissionManager>();
            if (await permManager.IsAllowedAsync(Types.PermissionType.GenerateOVCCodes, Context.User))
            {
                if (!Context.IsPrivate)
                {
                    await ReplyAsync("Ownership Verification Codes are confidential. They will be therefore sent via DM.");
                }
                var channel = await Context.User.GetOrCreateDMChannelAsync();
                await channel.SendMessageAsync($"The requested Ownership Verification Code for '{nationName}' is: {GenerateCode()}");
            }
            else
            {
                await ReplyAsync(AppSettings._permissionDeniedResponse);
            }
        }

        [Command("evc"), Summary("Returns an number of election verification codes")]
        public async Task DoGenerateEvcAsync(int count)
        {
            var permManager = Program.ServiceProvider.GetService<IPermissionManager>();
            if (await permManager.IsBotAdminAsync(Context.User) && Context.IsPrivate)
            {
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 1; i <= count; i++)
                {
                    stringBuilder.AppendLine(GenerateCode());
                    if (i % 25 == 0)
                    {
                        await ReplyAsync(stringBuilder.ToString());
                        stringBuilder.Clear();
                    }
                }
                if (count < 25 || count % 25 != 0)
                {
                    await ReplyAsync(stringBuilder.ToString());
                }
            }
        }

        public string GenerateCode()
        {
            StringBuilder resultBuilder = new StringBuilder();
            var _rnd = new Random();
            for (int i = 1; i < 16; i++)
            {
                var val = _rnd.Next(36);
                resultBuilder.Append(Base36.Encode(val).ToUpper());
                if (i % 5 == 0 && i != 15)
                {
                    resultBuilder.Append('-');
                }
            }
            return resultBuilder.ToString();
        }
    }

    /// <summary>
    /// A Base36 De- and Encoder
    /// </summary>
    public static class Base36
    {
        private const string CharList = "0123456789abcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// Encode the given number into a Base36 string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Encode(long input)
        {
            if (input < 0)
                throw new ArgumentOutOfRangeException("input", input, "input cannot be negative");

            char[] clistarr = CharList.ToCharArray();
            var result = new Stack<char>();
            while (input != 0)
            {
                result.Push(clistarr[input % 36]);
                input /= 36;
            }
            return new string(result.ToArray());
        }

        /// <summary>
        /// Decode the Base36 Encoded string into a number
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Int64 Decode(string input)
        {
            var reversed = input.ToLower().Reverse();
            long result = 0;
            int pos = 0;
            foreach (char c in reversed)
            {
                result += CharList.IndexOf(c) * (long) Math.Pow(36, pos);
                pos++;
            }
            return result;
        }
    }
}