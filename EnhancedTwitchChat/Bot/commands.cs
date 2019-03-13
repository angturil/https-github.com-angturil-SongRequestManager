using EnhancedTwitchChat.Chat;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

// Feature requests: Add Reason for being banned to banlist

namespace EnhancedTwitchChat.Bot
{
    public partial class RequestBot : MonoBehaviour
    {

        #region NEW Command Processor

        private void InitializeCommands()
        {

            // Note: Default permissions are broadcaster only, so don't need to set them
            // These settings need to be able to reconstruct  

            // Note, this really should pass the alias list instead of adding 3 commmands.
            foreach (string c in Config.Instance.RequestCommandAliases.Split(',').Distinct())
            {
                AddCommand(c, ProcessSongRequest, Everyone, "usage: %alias%<songname> or <song id>, omit <,>'s. %|%This adds a song to the request queue. Try and be a little specific. You can look up songs on %beatsaver%", _atleast1);
                Plugin.Log($"Added command alias \"{c}\" for song requests.");
            }

            // BUG / or is it a feature? Currently, commands are incorrectly(?) Being added as separate copies per alias, instead of referring to a single shared object. This means each subcommand actually only modifies its own settings. Will fix at a later date. This only affects commands with aliases. 
            // Since the only user modifiable fields atm are flags and help message, we can hack a fix though the appropriate functions to set those, to replicate those changes across alias copies.. ugh. 



            AddCommand("queue", ListQueue, Everyone, "usage: %alias%%|% ... Displays a list of the currently requested songs.", _nothing);

            AddCommand("unblock", Unban, Mod, "usage: %alias%<song id>, do not include <,>'s.", _beatsaversong);

            AddCommand("block", Ban, Mod, "usage: %alias%<song id>, do not include <,>'s.", _beatsaversong);

            AddCommand("remove", DequeueSong, Mod, "usage: %alias%<songname>,<username>,<song id> %|%... Removes a song from the queue.", _atleast1);

            AddCommand("clearqueue", Clearqueue, Broadcasteronly, "usage: %alias%%|%... Clears the song request queue. You can still get it back from the JustCleared deck, or the history window", _nothing);

            AddCommand("mtt", MoveRequestToTop, Mod, "usage: %alias%<songname>,<username>,<song id> %|%... Moves a song to the top of the request queue.", _atleast1);

            AddCommand("remap", Remap, Mod, "usage: %alias%<songid1> , <songid2>%|%... Remaps future song requests of <songid1> to <songid2> , hopefully a newer/better version of the map.", _RemapRegex);

            AddCommand("unmap", Unmap, Mod, "usage: %alias%<songid> %|%... Remove future remaps for songid.", _beatsaversong);

            AddCommand(new string[] { "lookup", "find" }, lookup, Mod | Sub | VIP, "usage: %alias%<song name> or <beatsaber id>, omit <>'s.%|%Get a list of songs from %beatsaver% matching your search criteria.", _atleast1);

            AddCommand(new string[] { "last", "demote", "later" }, MoveRequestToBottom, Mod, "usage: %alias%<songname>,<username>,<song id> %|%... Moves a song to the bottom of the request queue.", _atleast1);

            AddCommand(new string[] { "wrongsong", "wrong", "oops" }, WrongSong, Everyone, "usage: %alias%%|%... Removes your last requested song form the queue. It can be requested again later.", _nothing);

            AddCommand("open", OpenQueue, Mod, "usage: %alias%%|%... Opens the queue allowing song requests.", _nothing);

            AddCommand("close", CloseQueue, Mod, "usage: %alias%%|%... Closes the request queue.", _nothing);

            AddCommand("restore", restoredeck, Broadcasteronly, "usage: %alias%%|%... Restores the request queue from the previous session. Only useful if you have persistent Queue turned off.", _nothing);

            AddCommand("commandlist", showCommandlist, Everyone, "usage: %alias%%|%... Displays all the bot commands available to you.", _nothing);

            AddCommand("played", ShowSongsplayed, Mod, "usage: %alias%%|%... Displays all the songs already played this session.", _nothing);

            AddCommand("blist", ShowBanList, Broadcasteronly, "usage: Don't use, it will spam chat!", _atleast1); // Purposely annoying to use, add a character after the command to make it happen 


            AddCommand("readdeck", Readdeck, Broadcasteronly, "usage: %alias", _alphaNumericRegex);
            AddCommand("writedeck", Writedeck, Broadcasteronly, "usage: %alias", _alphaNumericRegex);

            AddCommand("clearalreadyplayed", ClearDuplicateList, Broadcasteronly, "usage: %alias%%|%... clears the list of already requested songs, allowing them to be requested again.", _nothing); // Needs a better name

            AddCommand("help", help, Everyone, "usage: %alias%<command name>, or just %alias%to show a list of all commands available to you.", _anything);

            AddCommand("link", ShowSongLink, Everyone, "usage: %alias%|%... Shows details, and a link to the current song", _nothing);

            AddCommand("allowmappers", MapperAllowList, Broadcasteronly, "usage: %alias%<mapper list> %|%... Selects the mapper list used by the AddNew command for adding the latest songs from %beatsaver%, filtered by the mapper list.", _alphaNumericRegex);  // The message needs better wording, but I don't feel like it right now

            AddCommand("chatmessage", ChatMessage, Broadcasteronly, "usage: %alias%<what you want to say in chat, supports % variables>", _atleast1); // BUG: Song support requires more intelligent %CurrentSong that correctly handles missing current song. Also, need a function to get the currenly playing song.
            AddCommand("runscript", RunScript, Broadcasteronly, "usage: %alias%<name>%|%Runs a script with a .script extension, no conditionals are allowed. startup.script will run when the bot is first started. Its probably best that you use an external editor to edit the scripts which are located in UserData/EnhancedTwitchChat", _atleast1);
            AddCommand("history", ShowHistory, Mod, "usage: %alias% %|% Shows a list of the recently played songs, starting from the most recent.", _nothing);

            AddCommand("att", AddToTop, Mod, "usage: %alias%<songname> or <song id>, omit <,>'s. %|%This adds a song to the top of the request queue. Try and be a little specific. You can look up songs on %beatsaver%", _atleast1);

            AddCommand("about", nop, Broadcasteronly, $"EnhancedTwitchChat Bot version 2.0.0. Developed by brian91292 and angturil. Find us on github.", _fail);



#if UNRELEASED


        // These are future features

        //AddCommand("who"); // Who requested a song (both in queue and in history)
        //AddCommand("alias"); // Create a command alias)
        //AddCommand("/at"); // Scehdule at a certain time (command property)
        //user history


        AddCommand("blockmappers", MapperBanList, Broadcasteronly, "usage: %alias%<mapper list> %|%... Selects a mapper list that will not be allowed in any song requests.", _alphaNumericRegex); // BUG: This code is behind a switch that can't be enabled yet.


            // Temporary commands for testing, most of these will be unified in a general list/parameter interface

            AddCommand(new string[] { "addnew", "addlatest" }, addNewSongs, Mod, "usage: %alias% <listname>%|%... Adds the latest maps from %beatsaver%, filtered by the previous selected allowmappers command", _nothing); // BUG: Note, need something to get the one of the true commands referenced, incases its renamed
            AddCommand("addsongs", addSongs, Broadcasteronly); // Basically search all, need to decide if its useful

            AddCommand("openlist", OpenList);
            AddCommand("unload", UnloadList);
            AddCommand("clearlist", ClearList);
            AddCommand("write", writelist);
            AddCommand("list", ListList);
            AddCommand("lists", showlists);
            AddCommand("addtolist", Addtolist, Broadcasteronly, "usage: %alias%<list> <value to add>", _atleast1);
            AddCommand("addtoqueue", queuelist, Broadcasteronly, "usage: %alias%<list>", _atleast1);

            AddCommand("removefromlist", RemoveFromlist, Broadcasteronly, "usage: %alias%<list> <value to add>", _atleast1);
            AddCommand("listundo", Addtolist, Broadcasteronly, "usage: %alias%<list>", _atleast1); // BUG: No function defined yet, undo the last operation

            AddCommand("deck", createdeck);
            AddCommand("unloaddeck", unloaddeck);
            AddCommand("loaddecks", loaddecks);
            AddCommand("decklist", decklist, Mod, "usage: %alias", _deck);
            AddCommand("whatdeck", whatdeck, Mod, "usage: %alias%<songid> or 'current'", _anything);
            AddCommand("mapper", addsongsbymapper, Broadcasteronly, "usage: %alias%<mapperlist>"); // This is actually most useful if we send it straight to list

            //AddCommand("test", LookupSongs);
#endif
        }



        // This code probably needs its own file
        // Some of these are just ideas, putting them all down, can filter them out later

        // Prototype code only
        public partial class BOTCOMMAND
        {

            public static List<BOTCOMMAND> cmdlist = new List<BOTCOMMAND>(); // Collection of our command objects
            static private Dictionary<string, int> aliaslist = new Dictionary<string, int>();

            public Action<TwitchUser, string> Method = null;  // Method to call
            public Action<TwitchUser, string, int, string> Method2 = null; // Alternate method

            public Action function = null;


            public CmdFlags rights;                  // flags
            public string ShortHelp;                   // short help text (on failing preliminary check
            public List<string> aliases;               // list of command aliases
            public Regex regexfilter;                 // reg ex filter to apply. For now, we're going to use a single string

            public string LongHelp; // Long help text
            public string HelpLink; // Help website link, Using a wikia might be the way to go
            public string permittedusers; // Name of list of permitted users.
            public string userParameter; // This is here incase I need it for some specific purpose
            public int userNumber;
            public int UseCount;  // Number of times command has been used, sadly without references, updating this is costly.

            public void SetPermittedUsers(string listname)
            {
                // BUG: Needs additional checking

                string fixedname = listname.ToLower();
                if (!fixedname.EndsWith(".users")) fixedname += ".users";
                permittedusers = fixedname;
            }

            public void Execute(ref TwitchUser user, ref string request, int flags, ref string Info)
            {
                if (Method2 != null) Method2(user, request, flags, Info);
                else if (Method != null) Method(user, request);

            }



            public void testcommand()
            {
                RequestBot.Instance.QueueChatMessage("hello world");
            }

            public void TestList(TwitchUser requestor, string request)
            {

                var msg = new QueueLongMessage();
                msg.Header("Loaded lists: ");
                foreach (var entry in listcollection.ListCollection) msg.Add($"{entry.Key} ({entry.Value.Count()})", ", ");
                msg.end("...", "No lists loaded.");
            }

            public BOTCOMMAND(Action<TwitchUser, string> method, CmdFlags flags, string shorthelptext, Regex regex, string[] alias)
            {
                Method = method;

                this.rights = flags;
                ShortHelp = shorthelptext;
                aliases = alias.ToList();
                LongHelp = "";
                HelpLink = "";
                permittedusers = "";
                if (regex == null)
                    regexfilter = _anything;
                else
                    regexfilter = regex;

                UseCount = 0;

                userParameter = "";
                userNumber = 0;

                foreach (var entry in aliases)
                {
                    if (!NewCommands.ContainsKey(entry)) // BUG: The moment this is called, we're committing this. Probably want to set more things before this happens. I'm not fixing it right now
                        {
                        if (!aliaslist.ContainsKey(entry)) aliaslist.Add(entry, cmdlist.Count);
                        NewCommands.Add(entry, this);
                        }
                    else
                    {
                        // BUG: Command alias is a duplicate
                    }
                }
            }



            // BUG: These are currently copies of each other (except the function prototype),I'm not 100% sure how to refactor. My previous attempt was not able to change the contents of the constructed object.
            public BOTCOMMAND(Action<TwitchUser, string, int, string> method, CmdFlags flags, string shorthelptext, Regex regex, string[] alias)
            {
                Method2 = method;
                this.rights = flags;
                ShortHelp = shorthelptext;
                aliases = alias.ToList();
                LongHelp = "";
                HelpLink = "";
                permittedusers = "";
                if (regex == null)
                    regexfilter = _anything;
                else
                    regexfilter = regex;

                UseCount = 0;

                userParameter = "";
                userNumber = 0;

                foreach (var entry in aliases)
                {
                    if (!NewCommands.ContainsKey(entry)) // BUG: The moment this is called, we're committing this. Probably want to set more things before this happens. I'm not fixing it right now
                        {
                        if (!aliaslist.ContainsKey(entry)) aliaslist.Add(entry, cmdlist.Count);
                        NewCommands.Add(entry, this);
                        }
                    else
                    {
                        // BUG: Command alias is a duplicate
                    }
                }

            }

            public static void Parse(TwitchUser user, string request, int flags = 0, string info = "")
            {
                if (!Instance) return;

                if (request.Length == 0) return; // Since we allow user configurable commands, blanks are a possibility

                if (request[0] != '!') return; // This won't always be here

                int commandstart = 1; // This is technically 0, right now we're setting it to 1 to maintain the ! behaviour
                int parameterstart = 1;

                //var match = Regex.Match(request, "^!(?<command>[^ ^/]*?<parameter>.*)");
                //string username = match.Success ? match.Groups["command"].Value : null;

                // This is a replacement for the much simpler Split code. It was changed to support /fakerest parameters, and sloppy users ... ie: !add4334-333 should now work, so should !command/flags
                while (parameterstart < request.Length && ((request[parameterstart] < '0' || request[parameterstart] > '9') && request[parameterstart] != '/' && request[parameterstart] != ' ')) parameterstart++;  // Command name ends with #... for now, I'll clean up some more later           
                int commandlength = parameterstart - commandstart;
                while (parameterstart < request.Length && request[parameterstart] == ' ') parameterstart++; // Eat the space(s) if that's the separator after the command

                if (commandlength == 0) return;

                string command = request.Substring(commandstart, commandlength).ToLower();
                if (NewCommands.ContainsKey(command))
                {
                    string param = request.Substring(parameterstart);

                    if (deck.ContainsKey(command))
                    {
                        if (param == "") param = command;
                        else
                        {
                            param = command + " " + param;
                        }
                    }

                    try
                    {
                        ExecuteCommand(command, ref user, param, flags, info);
                    }
                    catch (Exception ex)
                    {
                        // Display failure message, and lock out command for a time period. Not yet.

                        Plugin.Log(ex.ToString());

                    }
                }
            }



        }


        public void AddCommand(string[] alias, Action<TwitchUser, string> method, CmdFlags flags = Broadcasteronly, string shorthelptext = "usage: [%alias] ... Rights: %rights", Regex regex = null)
        {
            BOTCOMMAND.cmdlist.Add(new BOTCOMMAND(method, flags, shorthelptext, regex, alias));
        }

        public void AddCommand(string alias, Action<TwitchUser, string> method, CmdFlags flags = Broadcasteronly, string shorthelptext = "usage: [%alias] ... Rights: %rights", Regex regex = null)
        {
            string[] list = new string[] { alias.ToLower() };
            BOTCOMMAND.cmdlist.Add(new BOTCOMMAND(method, flags, shorthelptext, regex, list));
        }

        public void AddCommand(string alias, Action<TwitchUser, string, int, string> method, CmdFlags flags = Broadcasteronly, string shorthelptext = "usage: [%alias] ... Rights: %rights", Regex regex = null)
        {

            string[] list = new string[] { alias.ToLower() };
            BOTCOMMAND.cmdlist.Add(new BOTCOMMAND(method, flags, shorthelptext, regex, list));

        }
        public void AddCommand(string[] alias, Action<TwitchUser, string, int, string> method, CmdFlags flags = Broadcasteronly, string shorthelptext = "usage: [%alias] ... Rights: %rights", Regex regex = null)
        {
            BOTCOMMAND.cmdlist.Add(new BOTCOMMAND(method, flags, shorthelptext, regex, alias));

        }



        // A much more general solution for extracting dymatic values into a text string. If we need to convert a text message to one containing local values, but the availability of those values varies by calling location
        // We thus build a table with only those values we have. 



        // BUG: This is actually part of botcmd, please move
        public static void ShowHelpMessage(ref BOTCOMMAND botcmd, ref TwitchUser user, string param, bool showlong)
        {
            if (botcmd.rights.HasFlag(CmdFlags.QuietFail) || botcmd.rights.HasFlag(CmdFlags.Disabled)) return; // Make sure we're allowed to show help

            new DynamicText().AddUser(ref user).AddBotCmd(ref botcmd).QueueMessage(ref botcmd.ShortHelp, showlong);

            return;
        }


        private void nop(TwitchUser requestor, string request)
        {
            // This is command does nothing, it can be used as a placeholder for help text aliases.
        }

        // Get help on a command
        private void help(TwitchUser requestor, string request)
        {
            if (request == "")
            {
                var msg = new QueueLongMessage();
                msg.Header("Usage: help < ");
                foreach (var entry in NewCommands)
                {
                    var botcmd = entry.Value;
                    if (HasRights(ref botcmd, ref requestor))
                        msg.Add($"{entry.Key}", " ");
                }
                msg.Add(">");
                msg.end("...", $"No commands available >");
                return;
            }
            if (NewCommands.ContainsKey(request.ToLower()))
            {
                var BotCmd = NewCommands[request.ToLower()];
                ShowHelpMessage(ref BotCmd, ref requestor, request, true);
            }
            else
            {
                QueueChatMessage($"Unable to find help for {request}.");
            }
        }

        public static bool HasRights(ref BOTCOMMAND botcmd, ref TwitchUser user)
        {
            if (botcmd.rights.HasFlag(CmdFlags.Disabled)) return false;
            if (botcmd.rights.HasFlag(CmdFlags.Everyone)) return true; // Not sure if this is the best approach actually, not worth thinking about right now
            if (user.isBroadcaster & botcmd.rights.HasFlag(CmdFlags.Broadcaster)) return true;
            if (user.isMod & botcmd.rights.HasFlag(CmdFlags.Mod)) return true;
            if (user.isSub & botcmd.rights.HasFlag(CmdFlags.Sub)) return true;
            if (user.isVip & botcmd.rights.HasFlag(CmdFlags.VIP)) return true;
            return false;

        }

        // You can modify commands using !allow !setflags !clearflags and !sethelp
        public static void ExecuteCommand(string command, ref TwitchUser user, string param, int commandflags = 0, string info = "")
        {
            BOTCOMMAND botcmd;

            if (!NewCommands.TryGetValue(command, out botcmd)) return; // Unknown command

            // BUG: This is prototype code, it will of course be replaced. This message will be removed when its no longer prototype code

            // Permissions for these sub commands will always be by Broadcaster,or the (BUG: Future feature) user list of the EnhancedTwitchBot command. Note command behaviour that alters with permission should treat userlist as an escalation to Broadcaster.
            // Since these are never meant for an end user, they are not going to be configurable.

            // Example: !challenge/allow myfriends
            //          !decklist/setflags SUB
            //          !lookup/sethelp usage: %alias%<song name or id>

            if (user.isBroadcaster && param.StartsWith("/"))
            {
                string[] parts = param.Split(new char[] { ' ', ',' }, 2);

                string subcommand = parts[0].ToLower();

                if (subcommand.StartsWith("/allow")) // 
                {
                    if (parts.Length > 1)
                    {
                        string key = parts[1].ToLower();
                        NewCommands[command].permittedusers = key;
                        Instance?.QueueChatMessage($"Permit custom userlist set to  {key}.");
                    }

                    return;
                }

                if (subcommand.StartsWith("/disable")) // 
                {
                    Instance?.QueueChatMessage($"{command} Disabled.");

                    NewCommands[command].rights |= CmdFlags.Disabled;

                    //botcmd.rights |= CmdFlags.Disabled;
                    //NewCommands[command] = botcmd;
                    return;
                }

                if (subcommand.StartsWith("/enable")) // 
                {

                    Instance?.QueueChatMessage($"{command} Enabled.");
                    //botcmd.rights &= ~CmdFlags.Disabled;
                    //NewCommands[command] = botcmd;

                    NewCommands[command].rights &= ~CmdFlags.Disabled; 

                    return;
                }


                if (subcommand.StartsWith("/sethelp")) // 
                {
                    if (parts.Length > 1)
                    {
                        NewCommands[command].ShortHelp = parts[1];

                        Instance?.QueueChatMessage($"{command} help: {parts[1]}");
                    }

                    return;
                }

                if (subcommand.StartsWith("/flags")) // 
                {
                    Instance?.QueueChatMessage($"{command} flags: {botcmd.rights.ToString()}");
                    return;
                }

                if (subcommand.StartsWith("/setflags")) // 
                {
                    if (parts.Length > 1)
                    {
                        string[] flags = parts[1].Split(new char[] { ' ', ',' });


                        CmdFlags flag;

                        //NewCommands[command].rights ;

                        // BUG: Not working yet

                        Instance?.QueueChatMessage($"Not implemented");
                    }
                    return;

                }

                if (subcommand.StartsWith("/silent"))
                {
                    // BUG: Making the output silent doesn't work yet.

                    param = parts[1]; // Eat the switch, allowing the command to continue
                }


            }

            if (botcmd.rights.HasFlag(CmdFlags.Disabled)) return; // Disabled commands fail silently

            // Check permissions first

            bool allow = HasRights(ref botcmd, ref user);

            if (!allow && !botcmd.rights.HasFlag(CmdFlags.BypassRights) && !listcollection.contains(ref botcmd.permittedusers, user.displayName.ToLower()))
            {
                CmdFlags twitchpermission = botcmd.rights & CmdFlags.TwitchLevel;
                if (!botcmd.rights.HasFlag(CmdFlags.SilentPreflight)) Instance?.QueueChatMessage($"{command} is restricted to {twitchpermission.ToString()}");
                return;
            }

            if (param == "?") // Handle per command help requests - If permitted.
            {
                ShowHelpMessage(ref botcmd, ref user, param, true);
                return;
            }



            // Check regex

            if (!botcmd.regexfilter.IsMatch(param))
            {
                ShowHelpMessage(ref botcmd, ref user, param, false);
                return;
            }


            try
            {
                botcmd.Execute(ref user, ref param, (int)commandflags, ref info); // Call the command
            }
            catch (Exception ex)
            {
                // Display failure message, and lock out command for a time period. Not yet.

                Plugin.Log(ex.ToString());

            }

        }
        #endregion

  

    }
}