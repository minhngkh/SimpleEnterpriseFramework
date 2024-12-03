public interface IUpdateCommandBuilder: IDisposable {
    public abstract IUpdateCommandBuilder SetTable(string table);
    public abstract IUpdateCommandBuilder SetCondition(dynamic conditions);
    public abstract IUpdateCommandBuilder SetUpdateStatement(dynamic updates);
    public abstract void Update();
}
