using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;

namespace ChatMacros
{
    [APIVersion(1,12)]
    public class PluginMain : TerrariaPlugin
    {
        #region overrides

        public override string Name
        { get { return "Command Macros and Lists"; } }

        public override string Author
        { get { return "Snirk Immington"; } }

        public override string Description
        { get { return "Handy tools in commands!"; } }

        public override Version Version
        { get { return new Version(1, 0); } }

        public PluginMain ( Main game ) : base(game)
        { Order = -25; } // must be first. Always.

        #endregion

        #region initialize

        public override void Initialize ( )
        {
            Hooks.ServerHooks.Chat += OnChat;
            Hooks.GameHooks.Initialize += OnInit;
        }

        protected override void Dispose ( bool disposing )
        {
            if (disposing)
            {
                Hooks.ServerHooks.Chat -= OnChat;
                Hooks.GameHooks.Initialize -= OnInit;
            }
            base.Dispose(disposing);
        }

        private void OnInit ( )
        { 
            Commands.ChatCommands.Add(new Command("macro", MacroInfo, "macroinfo", "macrohelp"));
            Commands.ChatCommands.Add(new Command("macro", MacroList, "macrolist", "listmacros"));
            Commands.ChatCommands.Add(new Command("macro", ListInfo, "listinfo"));
        }

        #endregion

        #region chat hook

        public void OnChat ( messageBuffer mess, int who, string thetext, System.ComponentModel.HandledEventArgs args )
        {
            try
            {
                if (args.Handled) return; // not gonna fnpig around with that

                if (thetext.StartsWith("/") && TShock.Players[who].Group.HasPermission("macro") 
                && ( thetext.Contains('<') || thetext.Contains('{') ) 
                && !thetext.StartsWith("/login") && !thetext.StartsWith("/register"))
                {
                    var tsply = TShock.Players[who];
                    string text = thetext.Remove(0, 1);

                    #region make sure the command exists

                    var parameters = ParseParameters(text);
                    var com = Commands.ChatCommands.FirstOrDefault(c => c.Names.Contains(parameters[1].ToLower()));

                    if (com == null || com.CanRun(tsply) 
                    || ( !com.AllowServer && !tsply.RealPlayer ) 
                    || tsply.AwaitingResponse.ContainsKey(parameters[0]))
                    { return; } // we will let TShock handle this naturally, allowing other plugins a chance at this as well :)

                    #endregion // this may as well be removed from the non-list part because I don't need it

                    var PosMacros = new List<string>();
                    var indexes = new List<int>();
                    //bool list = false; 

                    // this epic layout parses in the macros and then the lists

                    #region handle dem macroz
                    if (text.Contains('<') && text.Contains('>')) // handle the macros
                    {
                        #region populate PosMacros
                        for (int total_i = 0; total_i < text.Length; total_i++)
                        {
                            if (text[total_i] == '<')
                            {
                                var posmac = "<"; indexes.Add(total_i + 1);
                                for (int word_i = total_i; word_i < text.Length; word_i++)
                                {
                                    if (text[word_i] != '>') // make the string
                                    {
                                        posmac += text[word_i];
                                    }
                                    else
                                    {
                                        posmac += '>';
                                        total_i += word_i - total_i; // word_i > total_i
                                        break;
                                    }
                                }
                                PosMacros.Add(posmac);
                            }
                        }
                        #endregion

                        var posplrs = new List<TSPlayer>();

                        #region foreach posmacro
                        for (int pos_i = 0; pos_i < PosMacros.Count; pos_i++)
                        {
                            var PosMac = PosMacros[pos_i]; var index = indexes[pos_i];
                            PosMac.Remove(0); PosMac.Remove(PosMac.Last()); // gets rid of "<>"

                            string name = "", args_str = ""; //var arguments = new List<string>(); 

                            // We're gonna let TShock and other plugins f around with the chat after we're done - we only specially do lists!

                            #region get the name and arguments
                            for (int name_i = 0; name_i < PosMac.Length; name_i++)
                            {
                                if (PosMac[name_i] != '(')
                                {
                                    name += PosMac[name_i];
                                }
                                else // we have name_i pointing to the index of the arguments
                                {
                                    for (int arg_i = name_i + 1; /*+1 account for '('*/ arg_i < PosMac.Length; arg_i++)
                                    {
                                        if (PosMac[arg_i] != ')')
                                        {
                                            args_str += PosMac[arg_i];
                                        }
                                        else break; // it is  ) so we're done
                                    }

                                    break; // this adds text from 0 to ( so we know the name
                                }
                            }
                            #endregion

                            string replacetext = "";

                            #region switch the names and handle text subbing
                            switch (name.ToLower())
                            {
                                #region case "ip":
                                case "ip":
                                {
                                    var matchedply = TShock.Utils.FindPlayer(args_str);

                                    if (matchedply.Count == 1)
                                    {
                                        replacetext = matchedply[0].IP;
                                        break;
                                    }
                                    else // basically an exception
                                    {
                                        tsply.SendErrorMessage("Macro <IP(player)> - replaced with the IP of player - " + matchedply.Count + " players matched!");
                                        return;
                                    }
                                }
                                #endregion ip

                                #region case "user": case "username":
                                case "user":
                                case "username":
                                {
                                    var matchedply = TShock.Utils.FindPlayer(args_str);

                                    if (matchedply.Count == 1)
                                    {
                                        if (matchedply[0].IsLoggedIn)
                                        {
                                            replacetext = matchedply[0].UserAccountName;
                                            break;
                                        }
                                        else // not logged in
                                        {
                                            tsply.SendErrorMessage("Macro <user(player)> - gets /login account name of player - " + matchedply[0].Name + " is not logged in!");
                                            return;
                                        }
                                    }
                                    else // not player matched
                                    {
                                        tsply.SendErrorMessage("Macro <user(player)> - gets the /login account name of the player - " + matchedply.Count + " players matched!");
                                        return;
                                    }
                                }
                                #endregion

                                #region case "group":
                                case "group":
                                {
                                    var matchedply = TShock.Utils.FindPlayer(args_str);

                                    if (matchedply.Count == 1)
                                    {
                                        if (matchedply[0].IsLoggedIn)
                                        {
                                            replacetext = matchedply[0].Group.Name;
                                            break;
                                        }
                                        else // not logged in
                                        {
                                            tsply.SendErrorMessage("Macro <group(player)> - gets the name of the TShock group the player's in - player is not logged in!");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        tsply.SendErrorMessage("Macro <group(player)> - gets the name of the TShock group the player's in - " + matchedply.Count + " players matched!");
                                        return;
                                    }
                                }
                                #endregion

                                #region //case "random":
                                //case "random":
                                //{
                                //    var rand = new Random();
                                //    switch (args_str.ToLower())
                                //    {
                                //        #region random player
                                //        case "ply":
                                //        case "player":
                                //        {
                                //            var ply = 
                                //            break;
                                //        }
                                //        #endregion

                                //        #region random npc
                                //        case "npc":
                                //        case "mob":
                                //        case "monster":
                                //        {
                                //            break;
                                //        }
                                //        #endregion

                                //        #region random item
                                //        case "item":
                                //        {
                                //            break;
                                //        }
                                //        #endregion

                                //        #region random warp
                                //        case "warp":
                                //        {
                                //            break;
                                //        }
                                //        #endregion
                                //    }
                                //    break;
                                //}
                                #endregion

                                #region //case "id":
                                //case "id":
                                //{
                                //    break;
                                //}
                                #endregion

                                #region case "topregion":
                                case "topregion":
                                {
                                    var region = TShock.Regions.GetTopRegion(TShock.Regions.InAreaRegion(tsply.TileX, tsply.TileY));
                                    if (region == null)
                                    {
                                        tsply.SendErrorMessage("Macro <topregion> - gets the top region at your position (Z value) - no regions at your position!");
                                        args.Handled = true; return;
                                    }
                                    else
                                    {
                                        replacetext = '"' + region.Name + '"';
                                        break;
                                    }
                                }
                                #endregion

                                // the list macros...

                                #region case "onteam":
                                case "onteam":
                                {
                                    byte team = 0;
                                    #region switch (args_str.ToLower())
                                    switch (args_str.ToLower())
                                    {
                                        case "white":
                                        case "none":
                                        case "off":
                                        {
                                            break;
                                        }

                                        case "red": { team = 1; break; }

                                        case "blue": { team = 2; break; }

                                        case "yellow": { team = 3; break; }

                                        case "green": { team = 4; break; }

                                        default:
                                        {
                                            tsply.SendErrorMessage("Macro <onteam(team)> - not a valid team."); args.Handled = true; return;
                                        }
                                    }
                                    #endregion

                                    var posply = TShock.Players.Where(p => p != null && p.RealPlayer && p.Team == team);

                                    if (posply.Count() == 0)
                                    {
                                        tsply.SendErrorMessage("Macro - <onteam(team)> - no players on the team!");
                                        args.Handled = true; return;
                                    }

                                    posplrs = posply.ToList();
                                    break;
                                }
                                #endregion

                                #region case "isgroup":
                                case "isgroup":
                                {
                                    var posgroup = TShock.Groups.GetGroupByName(args_str);

                                    if (posgroup == null)
                                    {
                                        tsply.SendErrorMessage("Macro <isgroup(group)> - no group matched! Case matters!");
                                        args.Handled = true; return;
                                    }

                                    var posply = TShock.Players.Where(p => p != null && p.RealPlayer && p.Group == posgroup);

                                    if (posply.Count() == 0)
                                    {
                                        tsply.SendErrorMessage("Macro <isgroup(group)> - no players in \"" + args_str + "\" are online!");
                                        args.Handled = true; return;
                                    }

                                    posplrs = posply.ToList();
                                    break;
                                }
                                #endregion

                                #region case "inregion":
                                case "inregion":
                                {
                                    //  <inregion(region)>

                                    var region = TShock.Regions.GetRegionByName(args_str);

                                    if (region == null)
                                    {
                                        tsply.SendErrorMessage("Macro <inregion(region)> - no region matched!");
                                        args.Handled = true; return;
                                    }

                                    // lambda expressions ftwwwwwwwwwwww
                                    var players = TShock.Players.Where(p => p != null && p.RealPlayer && TShock.Regions.InAreaRegion(p.TileX, p.TileY).Contains(region));

                                    if (players.Count() == 0)
                                    {
                                        tsply.SendErrorMessage("Macro <inregion(region)> - no players in region " + args_str + "!");
                                        args.Handled = true; return;
                                    }

                                    posplrs = players.ToList();
                                    break;
                                }
                                #endregion

                                #region case "distance":
                                case "distance":
                                {
                                    //  <distance(number)>

                                    int distance = 50;
                                    if (!int.TryParse(args_str, out distance))
                                    {
                                        tsply.SendErrorMessage("Macro <distance(number)> - must be a valid number!");
                                        args.Handled = true; return;
                                    }

                                    var players = TShock.Players.Where(p => p != null && p.RealPlayer && 
                                    Math.Sqrt(Math.Pow(p.TileX - tsply.TileX, 2) + 
                                              Math.Pow(p.TileY - tsply.TileY, 2)) <= distance); // distance function

                                    if (players.Count() == 0)
                                    {
                                        tsply.SendErrorMessage("Macro: <distance(number)> - no players are within this distance!");
                                        args.Handled = true; return;
                                    }

                                    posplrs = players.ToList();

                                    break;
                                }
                                #endregion

                                #region case "haspermission":
                                case "haspermission":
                                case "hasperm":
                                {
                                    var players = TShock.Players.Where(p => p != null && p.RealPlayer && p.Group.HasPermission(args_str));

                                    if (players.Count() == 0)
                                    {
                                        tsply.SendErrorMessage("Macro <hasperm(permission)> - no players with " + args_str + " matched! Make sure it's a valid permission.");
                                        args.Handled = true; return;
                                    }

                                    posplrs = players.ToList();
                                    break;
                                }
                                #endregion

                                #region case "pvp":
                                case "pvp":
                                {
                                    #region get PvP state
                                    bool pvp = false;
                                    switch (args_str.ToLower())
                                    {
                                        case "enabled":
                                        case "true":
                                        case "on": pvp = true; break;

                                        case "disabled":
                                        case "false":
                                        case "off": break;

                                        default: tsply.SendErrorMessage("Macro <pvp(\"on\"|\"off\") - must have a valid PvP state!");
                                        args.Handled = true; return;
                                    }
                                    #endregion

                                    var players = TShock.Players.Where(p => p != null && p.RealPlayer && p.TPlayer.hostile == pvp);

                                    if (players.Count() == 0)
                                    {
                                        tsply.SendErrorMessage("Macro <pvp(on|off)> - no players with that PvP status!");
                                        args.Handled = true; return;
                                    }

                                    posplrs = players.ToList(); break;
                                }
                                #endregion

                                #region case "namehas":
                                case "namehas":
                                case "nameincludes":
                                case "namecontains":
                                {
                                    var players = TShock.Players.Where(p => p != null && p.RealPlayer && p.Name.ToLower().Contains(args_str.ToLower()));

                                    if (players.Count() == 0)
                                    {
                                        tsply.SendErrorMessage("Macro <namehas(partialname)> - no players matched!");
                                        args.Handled = true; return;
                                    }

                                    posplrs = players.ToList(); break;
                                }
                                #endregion

                                #region default:
                                #endregion
                            }
                            #endregion switch

                            #region set up list

                            if (posplrs.Count != 0) // list time y'all
                            {
                                var list = "{" + string.Join(",", posplrs.ConvertAll(p => p.Name)) + "}";

                                text.Replace("<" +PosMacros[pos_i] + ">", list); // yeee budy (2)
                            }

                            #endregion
                        }
                        #endregion foreach poslist
                    }
                    #endregion macroz

                    #region handle lists -> macros need to add "{text}"
                    if (text.Contains('{') && text.Contains('}'))
                    {
                        args.Handled = true; // ohhooo hoh hoooh ooh

                        // no mute handler - that's in commands

                        int list_start = 0; string list_str = "";
                        var list_player = new List<TSPlayer>();
                        //  we have the text, we're looking for the list!

                        #region get the string
                        for (int list_i = 0; list_i < text.Length; list_i++)
                        {
                            if (text[list_i] == '{')
                            {
                                list_start = list_i + 1;

                                for (int loop_i = list_i+1; loop_i < text.Length; loop_i++)
                                {
                                    if (text[list_i] != '}')
                                    {
                                        list_str += text[list_i];
                                    }
                                    else break;
                                }

                                break;
                            }
                        }
                        #endregion

                        // so now we have the string list_str that is the names with commas

                        var list_split = list_str.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        if (list_split.Length == 0)
                        {
                            tsply.SendErrorMessage("List - no players in the list! Use /listhelp for info on lists."); return;
                        }

                        #region check and add players
                        foreach (var str in list_split)
                        {
                            var plr = TShock.Utils.FindPlayer(str);

                            if (plr.Count != 1)
                            {
                                tsply.SendErrorMessage("List - name - name \"" + str + "\" not a valid player!"); return;
                            }
                            else list_player.Add(plr[0]);
                        }
                        #endregion

                        var newcomstring = text.Remove(list_start, list_str.Length);
                        var newtext = newcomstring.Insert(list_start, "{0}"); // haha string.Format FTWWWW
                        var replacestrings = new List<string>(); string playernames = "Players: ";

                        #region set up player name for each command string
                        foreach (var ply in list_player)
                        {
                            string replacename = ply.Name;
                            playernames += replacename + " | "; // adds to the logged thing
                            if (ply.Name.Contains(' '))
                            {
                                replacename = '"' + replacename + '"';
                            }

                            replacestrings.Add(text.SFormat(replacename));
                        }
                        #endregion

                        if (list_player.Count > 1) playernames += "(" + list_player.Count + ")";

                        if (com.DoLog)
                            TShock.Utils.SendLogs("{0} executed [{1}]. {2}".SFormat(tsply.Name, thetext, playernames), Color.Red);

                        foreach (var comstr in replacestrings)
                        {
                            com.Run(comstr, tsply, ParseParameters(comstr));
                        }
                    }
                    #endregion
                }
            }
            catch (Exception)
            {
                TShock.Players[who].SendErrorMessage("An error occured while trying do do a macro function, check logs for details."); 
                args.Handled = true; // honsetly there's nothing else anyone can do may as well stop hendling command....
            }
        }

        #endregion

        #region commands for info

        private static void ListInfo ( CommandArgs com )
        {
            com.Player.SendInfoMessage("A list of players can be achieved with braces - { } around the players' names which are seperated by commas.");
            com.Player.SendInfoMessage("Lists can also be achieved with macros (see /macroinfo) - for example <inRegion(region name)> (players in a region)");
            com.Player.SendInfoMessage("Example: /w {bob,mr. joe,al} hi -> does /w bob hi THEN /w \"Mr. Joe\" hi THEN /w Alex hi -> really useful.");
            com.Player.SendInfoMessage("A macro: /w <group(guest)> hi -> gets a list of players in the group \"guest\" and does /w player hi.");
            com.Player.SendInfoMessage("For more info on macros, use /macroinfo. For lists of macros use /macrolist");
        }

        private static void MacroInfo ( CommandArgs com )
        {
            com.Player.SendInfoMessage("Command Macros is a utility plugin designed to make complex commands easier. They're little functions you can do in-command:");
            com.Player.SendInfoMessage("You can write one in a command as one of the parameters (words) in the command. Some of these marcos have parameters.");
            com.Player.SendInfoMessage("Macros will replace your text with names of players, data, and more. Some can be used to get lists of players (info in /listinfo)");
            com.Player.SendInfoMessage("Macros are sperate from text by using <> and () in parameters. For example, /banip <IP(Snirk)> - this becomes /banip 127.0.0.1 (example)");
            com.Player.SendInfoMessage("Or: /region protect <topregion> false - removes protection on the top region at your position. For a list of macros, use /macrolist.");
        }

        private static void MacroList ( CommandArgs com )
        {
            //  /macros <list|number> [list number]

            if (com.Parameters.Count == 0 || com.Parameters[0] == "1")
            {
                com.Player.SendInfoMessage("");
            }

            #region code
            //if (com.Parameters.Count < 2 || com.Parameters[0] == "1")
            //{
            //    com.Player.SendSuccessMessage("Macros page 1. Remember, case does not matter in the macro name. Macro - explanation - example(s)");
            //    com.Player.SendInfoMessage("<IP(player)> - player's IP address - /banip");
            //    com.Player.SendInfoMessage("<userName(player)> - player's /login name (or sends error message if player not logged in) - /region allow, /user group");
            //    com.Player.SendInfoMessage("<group(player)> - player's group (or sends error message if player not logged in) - /modgroup /region allowg");
            //    com.Player.SendInfoMessage("Type /macrolist 2 for the other command macros (out of 2 pages).");
            //}
            //else
            //{
            //    if (com.Parameters[0] == "2")
            //    {
            //        com.Player.SendSuccessMessage("Macros page 2. Remember, case does not matter in the macro name. Macro - explanation - example(s)");
            //        com.Player.SendInfoMessage("<topregion> - the name of the top region (z value) you're standing on (or error message if no region) - /region");
            //        com.Player.SendInfoMessage("For macros that do commands to lists of players, use /macrolist");
            //    }
            //}
            #endregion
        }

        #endregion

        #region Utilities functions

        /// <summary>
        /// Parses a string of parameters into a list. Handles quotes.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static List<String> ParseParameters ( string str )
        {
            var ret = new List<string>();
            var sb = new StringBuilder();
            bool instr = false;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (instr)
                {
                    if (c == '\\')
                    {
                        if (i + 1 >= str.Length) break;
                        c = GetEscape(str[++i]);
                    }
                    else if (c == '"')
                    {
                        ret.Add(sb.ToString());
                        sb.Clear();
                        instr = false;
                        continue;
                    }
                    sb.Append(c);
                }
                else
                {
                    if (IsWhiteSpace(c))
                    {
                        if (sb.Length > 0)
                        {
                            ret.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                    else if (c == '"')
                    {
                        if (sb.Length > 0)
                        {
                            ret.Add(sb.ToString());
                            sb.Clear();
                        }
                        instr = true;
                    }
                    else sb.Append(c);
                }
            }
            if (sb.Length > 0) ret.Add(sb.ToString());

            return ret;
        }

        private static char GetEscape ( char c )
        {
            switch (c)
            {
                case '\\':
                return '\\';
                case '"':
                return '"';
                case 't':
                return '\t';
                default:
                return c;
            }
        }

        private static bool IsWhiteSpace ( char c )
        {
            return c == ' ' || c == '\t' || c == '\n';
        }

        #endregion
    }
}
