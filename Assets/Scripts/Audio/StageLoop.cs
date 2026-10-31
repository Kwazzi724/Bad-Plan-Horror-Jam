using System;

[Serializable]
public struct StageLoop
{
    public int stage;
    public int loop;

    public StageLoop(int stage, int loop)
    {
        this.stage = stage;
        this.loop = loop;
    }
}