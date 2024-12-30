namespace SEF.Repository;

public interface IModel {
    public string TableName {get;}
    public (string, object?)[] GetPairs();
}
