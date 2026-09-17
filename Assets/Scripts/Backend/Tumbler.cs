using UnityEngine;
using System.Collections.Generic;

/// <summary>One cascade step: the screen before/after removal, gravity and refill, plus its wins.</summary>
public class TumbleInfo
{
    public List<List<int>> landed = null;     // screen as it landed (before removal)
    public List<List<int>> withHoles = null;  // winning symbols replaced by EMPTY
    public List<List<int>> settled = null;    // after gravity, holes at the top
    public List<List<int>> merged = null;     // after refill from the reel strip
    public List<WinLineInfo> lines = null;    // wins evaluated on 'landed'

    public List<List<MoveInfo>> dropMoves = null;
    public List<List<MoveInfo>> tumbleMoves = null;
    public double wins = 0;
    public int cascades = 0;
}

public class MoveInfo
{
    public int id = 0;
    public int start = -1;
    public int end = -1;
    public int len = -1;

    public MoveInfo(int id) { this.id = id; }
    public bool IsMoving() => start != end;
}

/// <summary>
/// Cascade engine. Loops: evaluate wins → remove winning symbols → gravity →
/// refill from the reel strip → re-evaluate, until a step pays nothing.
/// Reads/writes server.screenSymbols and server.stopPositions directly.
/// </summary>
public class Tumbler
{
    protected int EMPTY_ID;
    protected int numberOfReels;
    protected int slotHeight;
    protected Server server;
    public bool isTumbling = false;

    public Tumbler(int numberOfReels, int slotHeight, int emptyId, Server server)
    {
        this.numberOfReels = numberOfReels;
        this.slotHeight = slotHeight;
        this.EMPTY_ID = emptyId;
        this.server = server;
    }

    public List<TumbleInfo> Do(bool simulationMode, List<List<int>> reelSet)
    {
        List<TumbleInfo> tumbles = new List<TumbleInfo>();
        bool tumble = true;

        PrintScreen("LANDED:", server.screenSymbols, simulationMode);

        while (tumble)
        {
            // 1. Evaluate wins on the current screen
            List<WinLineInfo> lines = server.ComputeWinsForScreen();
            List<List<int>> landedScreen = CopySymbols(server.screenSymbols);

            double stepWin = 0;
            foreach (var line in lines)
                stepWin += line.win;

            tumble = stepWin > 0;
            isTumbling = tumble;
            if (!tumble)
                break;

            // 2. Remove winning symbols, then drop + refill
            server.screenSymbols = RemoveWinningSymbols(server.screenSymbols, lines);
            TumbleInfo info = Tumble(simulationMode, reelSet);

            info.landed = landedScreen;
            info.lines = lines;
            info.wins = stepWin;

            server.screenSymbols = CopySymbols(info.merged);
            tumbles.Add(info);
        }

        return tumbles;
    }

    public double GetWins(List<TumbleInfo> tumbles)
    {
        double wins = 0;
        foreach (var t in tumbles) wins += t.wins;
        return wins;
    }

    public int GetCascadedSymbolsCount(List<TumbleInfo> tumbles)
    {
        int count = 0;
        foreach (var t in tumbles)
        {
            int sum = 0;
            foreach (var line in t.lines) sum += line.Positions.Length;
            t.cascades = sum;
            count += sum;
        }
        return count;
    }

    protected TumbleInfo Tumble(bool simulationMode, List<List<int>> reelSet)
    {
        TumbleInfo info = new TumbleInfo();

        PrintScreen("HOLES:", server.screenSymbols, simulationMode);
        info.withHoles = CopySymbols(server.screenSymbols);

        List<List<MoveInfo>> dropMoves = DropSymbolsInBoard(server.screenSymbols);
        server.screenSymbols = SettleSymbols(server.screenSymbols, dropMoves);
        PrintScreen("SETTLED:", server.screenSymbols, simulationMode);

        info.dropMoves = dropMoves;
        info.settled = CopySymbols(server.screenSymbols);

        // stopPositions is a reference; decrements persist across cascade steps
        List<List<int>> fresh = ComputeTumbleScreenSymbols(server.screenSymbols, server.stopPositions, reelSet);
        PrintScreen("REFILL:", fresh, simulationMode);

        server.screenSymbols = MergeTumbleSymbols(fresh, server.screenSymbols);
        PrintScreen("MERGED:", server.screenSymbols, simulationMode);

        info.merged = CopySymbols(server.screenSymbols);
        info.tumbleMoves = DropFreshSymbolsInBoard(fresh);
        return info;
    }

    protected List<List<MoveInfo>> CreateEmptyMovements()
    {
        var movements = new List<List<MoveInfo>>();
        for (int reel = 0; reel < numberOfReels; reel++)
        {
            movements.Add(new List<MoveInfo>());
            for (int line = 0; line < slotHeight; line++)
                movements[reel].Add(new MoveInfo(-1));
        }
        return movements;
    }

    /// <summary>Computes how far each surviving symbol falls to fill holes below it.</summary>
    protected List<List<MoveInfo>> DropSymbolsInBoard(List<List<int>> symbols)
    {
        var movements = CreateEmptyMovements();
        var copy = CopySymbols(symbols);

        for (int reel = 0; reel < numberOfReels; reel++)
        {
            for (int line = slotHeight - 1; line >= 0; line--)
            {
                int id = copy[reel][line];
                MoveInfo move = movements[reel][line];
                move.id = id;
                move.start = move.end = line * numberOfReels + reel;

                if (id == EMPTY_ID) continue;

                for (int drop = line + 1; drop < slotHeight; drop++)
                {
                    if (copy[reel][drop] == EMPTY_ID)
                    {
                        copy[reel][drop - 1] = EMPTY_ID;
                        copy[reel][drop] = id;
                        move.end = drop * numberOfReels + reel;
                    }
                }
            }
        }
        return movements;
    }

    protected List<List<MoveInfo>> DropFreshSymbolsInBoard(List<List<int>> symbols)
    {
        var movements = CreateEmptyMovements();
        int height = 0;
        for (int reel = 0; reel < symbols.Count; reel++)
        {
            int h = 0;
            foreach (var s in symbols[reel]) if (s != EMPTY_ID) h++;
            height = Mathf.Max(height, h);
        }

        for (int reel = 0; reel < symbols.Count; reel++)
        {
            for (int line = 0; line < symbols[reel].Count; line++)
            {
                MoveInfo move = movements[reel][line];
                move.id = symbols[reel][line];
                move.end = line * numberOfReels + reel;
                move.len = height;
            }
        }
        return movements;
    }

    protected List<List<int>> SettleSymbols(List<List<int>> symbols, List<List<MoveInfo>> movements)
    {
        var screen = new List<List<int>>();
        for (int reel = 0; reel < numberOfReels; reel++)
        {
            screen.Add(new List<int>());
            for (int line = 0; line < symbols[reel].Count; line++)
                screen[reel].Add(EMPTY_ID);
        }

        // The grid is pre-filled with EMPTY; only place real symbols at their landing
        // positions. Empty cells must not be written back or they can overwrite a
        // symbol that has already dropped into that spot.
        foreach (var reelMoves in movements)
        {
            foreach (var move in reelMoves)
            {
                if (move.id == EMPTY_ID) continue;

                int endLine = move.end / numberOfReels;
                int endReel = move.end % numberOfReels;
                screen[endReel][endLine] = move.id;
            }
        }
        return screen;
    }

    protected List<List<int>> RemoveWinningSymbols(List<List<int>> screenSymbols, List<WinLineInfo> lines)
    {
        var screen = CopySymbols(screenSymbols);
        foreach (var line in lines)
        {
            foreach (int position in line.Positions)
            {
                int row = position / numberOfReels;
                int reel = position % numberOfReels;
                screen[reel][row] = EMPTY_ID;
            }
        }
        return screen;
    }

    protected List<List<int>> CopySymbols(List<List<int>> symbols)
    {
        var screen = new List<List<int>>();
        foreach (var reel in symbols)
            screen.Add(new List<int>(reel));
        return screen;
    }

    /// <summary>Pulls new symbols from the strip (walking stop positions backwards) to fill the holes of each reel.</summary>
    protected List<List<int>> ComputeTumbleScreenSymbols(List<List<int>> symbols, List<int> stopPositions, List<List<int>> reelSet)
    {
        var screen = new List<List<int>>();
        for (int reel = 0; reel < symbols.Count; reel++)
        {
            int toAdd = 0;
            foreach (var s in symbols[reel]) if (s == EMPTY_ID) toAdd++;

            screen.Add(new List<int>());
            while (toAdd-- > 0)
            {
                stopPositions[reel]--;
                if (stopPositions[reel] < 0)
                    stopPositions[reel] = reelSet[reel].Count - 1;
                screen[reel].Add(reelSet[reel][stopPositions[reel]]);
            }
            screen[reel].Reverse();
        }
        return screen;
    }

    protected List<List<int>> MergeTumbleSymbols(List<List<int>> fresh, List<List<int>> into)
    {
        for (int reel = 0; reel < fresh.Count; reel++)
            for (int line = 0; line < fresh[reel].Count; line++)
                into[reel][line] = fresh[reel][line];
        return into;
    }

    protected void PrintScreen(string title, List<List<int>> symbols, bool simulationMode)
    {
        if (simulationMode) return;

        string message = title + "\n";
        for (int line = 0; line < slotHeight; line++)
        {
            for (int reel = 0; reel < numberOfReels; reel++)
            {
                if (line < symbols[reel].Count)
                {
                    int id = symbols[reel][line];
                    message += (id == EMPTY_ID ? " " : Server.SymbolNames[id]) + "\t";
                }
            }
            message += "\n";
        }
        Debug.Log(message + "========================================\n");
    }
}
