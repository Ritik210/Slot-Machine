using System.Collections.Generic;

public class WaysUtil: ISlotMode
{
    public List<int> invalidSymbols = null;

    private int reelsNo = -1;
    private int linesNo = -1;
    private int dominatingSymbolDefault = -1;
    private int[][] allWaysDef = null;

    private List<int[]> winningWays = null;

    private Server server = null;

    public WaysUtil(int reelsNo, int linesNo, Server server)
    {
        this.reelsNo = reelsNo;
        this.linesNo = linesNo;
        this.server = server;
    }

    public int computeMaxWaysNumber()
    {
        int result = 1;
        for (int i = 0; i < reelsNo; i++)
        {
            result *= linesNo;
        }
        return result;
    }

    public int[][] ComputeAllWays()
    {
        allWaysDef = new int[computeMaxWaysNumber()][];
        int totalLines = computeWays(0, new int[reelsNo], 0, 0, 1);
        return allWaysDef;
    }

    //recursive function which computes all possible routes given a starting column and direction
    private int computeWays(int column, int[] wayPos, int currentPos, int lineID, int direction)
    {
        if (column < reelsNo && column >= 0)
        {
            for (int line = 0; line < linesNo; line++)
            {
                wayPos[currentPos] = line * reelsNo + column;
                lineID = computeWays(column + direction, wayPos, currentPos + 1, lineID, direction);
            }
        }
        else
        {
            allWaysDef[lineID] = (int[])wayPos.Clone();
            lineID++;
        }
        return lineID;
    }

    public int[][] extractWinningWays(int startColumn, int minWaySize, int direction)
    {
        winningWays = new List<int[]>();
        computeWinningWays(startColumn, 0, dominatingSymbolDefault, minWaySize, direction, new int[reelsNo]);

        return winningWays.ToArray();
    }

    protected bool computeWinningWays(int column, int currentPos, int dominatingSymbol, int minSize, int direction, int[] wayPos)
    {
        int inputDominatingSymbol = dominatingSymbol;
        bool longerWayExists = false;

        if (wayPos == null)
        {
            wayPos = new int[reelsNo];
        }

        if (column < reelsNo && column >= 0)
        {
            for (int line = 0; line < linesNo; line++)
            {
                if (validCell(line, column))
                {
                    int symbol = server.GetReelSymbol(line, column);

                    if (!server.GetSymbolIsWild(symbol) && dominatingSymbol == dominatingSymbolDefault)
                    {
                        dominatingSymbol = symbol;
                    }

                    if (symbol == dominatingSymbol || server.GetSymbolIsWild(symbol))
                    {
                        wayPos[currentPos] = line * reelsNo + column;

                        longerWayExists = computeWinningWays(column + direction, currentPos + 1, dominatingSymbol, minSize, direction, wayPos) || longerWayExists;
                    }

                    dominatingSymbol = inputDominatingSymbol;
                }
            }
            if (!longerWayExists && currentPos >= minSize)
            {
                winningWays.Add(sliceArray(wayPos, 0, currentPos));
                longerWayExists = true;
            }
        }
        else
        {
            winningWays.Add(sliceArray(wayPos, 0, currentPos));
            longerWayExists = true;
        }

        return longerWayExists;
    }

    protected virtual bool validCell(int line, int reel)
    {
        if (invalidSymbols != null)
        {
            return invalidSymbols.IndexOf(server.GetReelSymbol(line, reel)) < 0;
        }

        return true;
    }

    protected int[] sliceArray(int[] array, int startIndex, int limit)
    {
        int[] result = new int[limit];
        for (int i = 0; i < limit; i++)
        {
            result[i] = array[startIndex + i];
        }

        return result;
    }

    public List<WinLineInfo> ComputeScreenWins()
    {
        int[][] linesDef = extractWinningWays(0, 3, 1);
        LinesUtil linesUtil = new LinesUtil(reelsNo, linesNo, server);
        return linesUtil.ComputeScreenLineWins(linesDef);
    }
}
