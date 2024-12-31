namespace SEF.UI;
using SEF.Repository;

public class UIForm<T> where T: class, new() {
    IRepository repo;
    public string TableName {get; private set;}

    public UIForm(IRepository repo) {
        this.repo = repo;
        this.TableName = typeof(T).Name;
    }

    public virtual void Add(T obj) {
        repo.Add(obj);
    }

    public virtual void Add(Dictionary<string, object> values) {
        repo.Add(TableName, values);
    }

    public virtual void Update(T oldObj, T newObj) {
        repo.UpdateRow(oldObj, newObj);
    }

    public virtual void Delete(T obj) {
        repo.DeleteRow(TableName, obj);
    }

    public virtual List<string> GetColumnNames() {
        return repo.ListColumns(TableName).Select(x => x.name).ToList();
    }

    public virtual List<T> GetAllData() {
        return repo.Find<T>();
    }
}