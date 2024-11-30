public interface IUpdateCommandBuilder: IDisposable {
    public abstract IUpdateCommandBuilder SetTable(string table);
    public abstract IUpdateCommandBuilder SetCondition(params (string, object)[] andConditions);
    public abstract IUpdateCommandBuilder SetUpdateStatement(params (string, object)[] fieldAndValues);
    public abstract void Update();
}
