using System.Collections.Generic;
using System.Linq;
public class LinesUtil : ISlotMode
{
    private Server server = null;
    private int numberOfReels = -1;
    private int slotHeight = -1;

    private string lineRules = "0,1,2,3,4~5,6,7,8,9~10,11,12,13,14~0,6,12,8,4~10,6,2,8,14~0,1,7,13,14~10,11,7,3,4~5,1,2,3,9~5,11,12,13,9~0,6,7,8,4~10,6,7,8,14~5,1,7,13,9~5,11,7,3,9~0,6,2,8,4~10,6,12,8,14~5,6,2,8,9~5,6,12,8,9~0,11,2,13,4~10,1,12,3,14~5,1,12,3,9";
    private List<List<int>> lineDefs = new List<List<int>>();
    

    public LinesUtil(int reelsNo, int linesNo, Server server)
    {
        numberOfReels = reelsNo;
        slotHeight = linesNo;
        this.server = server;

        ParseLinesDef();
    }

    private void ParseLinesDef()
    {
        var rules = lineRules.Split('~');
        foreach (var rule in rules)
        {
            lineDefs.Add(new List<int>());
            var positions = rule.Split(',');
            for (int i = 0; i < positions.Length; i++)
            {
                lineDefs[lineDefs.Count - 1].Add(int.Parse(positions[i]));
            }
        }
    }

    public List<WinLineInfo> ComputeScreenLineWins(IEnumerable<IEnumerable<int>> lDefs)
    {
        var lDef = lDefs.Select(row => row.ToList()).ToList();

        var payoutData = server.GetPayoutData();
        var betLines = new WinLineInfo[lDef.Count];

        for (int i = 0; i < lDef.Count; i++)
        {
            betLines[i] = new WinLineInfo();
            betLines[i].Id = i + 1;
            betLines[i].Positions = new int[lDef[i].Count];
            betLines[i].containsWild = false;

            for (int j = 0; j < lDef[i].Count; j++)
            {
                betLines[i].Positions[j] = lDef[i][j];
            }
        }

        //Process active lines
        for (int betLinesIdx = 0; betLinesIdx < betLines.Length; betLinesIdx++)
        {
            //	bool scatterIsWild = XT.GetBool(Vars.ScatterSymbolIsWild);

            //	//Trim the winlines
            int winningsymbol = 2; //we assume wild is the winner
            int actualwinningsymbol = -1; //it's never wild, for PossibleWinComboPerSymbol

            int leadingWilds = 0;
            //we parse the line to find the symbol that generated the win
            for (int i = 0; i < betLines[betLinesIdx].Positions.Length; i++)
            {
                int pos = betLines[betLinesIdx].Positions[i];
                int reelIdx = pos % numberOfReels;
                int reelPosIdx = pos / numberOfReels;
                int symbol = server.GetReelSymbol(reelPosIdx, reelIdx);

                if (actualwinningsymbol == -1)
                    actualwinningsymbol = symbol;

                if (!server.GetSymbolIsWild(symbol))
                {
                    if (((symbol != winningsymbol) && (!server.GetSymbolIsWild(winningsymbol))) || (symbol < 2)) //Stop the line at this point
                    {
                        int newLength = i;

                        if (leadingWilds > 0)
                            if (payoutData[2][leadingWilds - 1] > payoutData[winningsymbol][newLength - 1])
                            {
                                newLength = leadingWilds;
                                winningsymbol = 2;
                            }

                        int[] newPos = new int[newLength];
                        for (int j = 0; j < newLength; j++)
                            newPos[j] = betLines[betLinesIdx].Positions[j];
                        betLines[betLinesIdx].Positions = newPos;
                    }
                    else
                    {
                        winningsymbol = symbol;
                        actualwinningsymbol = symbol;
                    }
                }
                else
                {
                    if (leadingWilds == i) //only increase if it was increased each loop
                        leadingWilds++;
                }

                if (!betLines[betLinesIdx].containsWild)
                    betLines[betLinesIdx].containsWild = server.GetSymbolIsWild(symbol);
            }

            betLines[betLinesIdx].dominatingSymbol = winningsymbol;
        }

        List<WinLineInfo> lineWins = new List<WinLineInfo>();
        foreach (WinLineInfo wli in betLines)
        {
            if (wli.Positions.Length >= 2 && payoutData[wli.dominatingSymbol][wli.Positions.Length - 1] > 0)
            {
                lineWins.Add(wli);
            }
        }
        //  return lineWins;

        List<WinLineInfo> winLines = lineWins;
        // Calculation for High Pay SYmbols
        // /*
        betLines = new WinLineInfo[lDef.Count];
        for (int i = 0; i < lDef.Count; i++)
        {
            betLines[i] = new WinLineInfo();
            betLines[i].Id = i + 1;
            betLines[i].Positions = new int[lDef[i].Count];
            betLines[i].containsWild = false;
            for (int j = 0; j < lDef[i].Count; j++)
            {
                betLines[i].Positions[j] = lDef[i][j];
            }
        }
        for (int betLinesIdx = 0; betLinesIdx < betLines.Length; betLinesIdx++)
        {
            betLines[betLinesIdx].dominatingSymbol = 2; // AH (anyhigh symbol Id)
            int newLength = 0;
            int leadingWild = 0;
            for (int i = 0; i < betLines[betLinesIdx].Positions.Length; i++)
            {
                int pos = betLines[betLinesIdx].Positions[i];
                int reelIdx = pos % numberOfReels;
                int reelPosIdx = pos / numberOfReels;
                int symbol = server.GetReelSymbol(reelPosIdx, reelIdx);
                if (leadingWild == i && server.GetSymbolIsWild(symbol))
                {
                    leadingWild++;
                }
                else
                {
                    break;
                }
            }
            if (leadingWild >= 3)
            {
                newLength = leadingWild;
            }
            else
            {
                newLength = 1;
            }
            int[] newPos = new int[newLength];
            for (int j = 0; j < newLength; j++)
                newPos[j] = betLines[betLinesIdx].Positions[j];
            betLines[betLinesIdx].Positions = newPos;
        }

        List<WinLineInfo> removedWinLines = new List<WinLineInfo>();

        // Remove extra line and include only Highest pay Line
        for (int i = 0; i < betLines.Length; i++)
        {
            if (betLines[i].Positions.Length > 2 && server.GetSymbolIsWild(betLines[i].dominatingSymbol))
            {
                foreach (WinLineInfo wli in winLines)
                {
                    if (wli.Id == betLines[i].Id)
                    {
                        if (payoutData[betLines[i].dominatingSymbol][betLines[i].Positions.Length - 1] > payoutData[wli.dominatingSymbol][wli.Positions.Length - 1])
                        {
                            removedWinLines.Add(wli);
                            // duplicate++;
                        }
                        else // if less than
                            betLines[i].Positions = new int[1];
                        break;
                    }
                }
            }
        }
        if (removedWinLines.Count > 0)
        {
            foreach (WinLineInfo wli in removedWinLines)
            {
                // duplicateRemove++;
                winLines.Remove(wli);
            }
        }
        for (int i = 0; i < betLines.Length; i++)
        {
            if ((betLines[i].Positions.Length > 2 && betLines[i].dominatingSymbol == 2) && payoutData[betLines[i].dominatingSymbol][betLines[i].Positions.Length - 1] > 0)
            {
                winLines.Add(betLines[i]);
            }
        }

        return winLines;
    }

    public List<WinLineInfo> ComputeScreenWins()
    {
        return ComputeScreenLineWins(lineDefs);
    }
}
