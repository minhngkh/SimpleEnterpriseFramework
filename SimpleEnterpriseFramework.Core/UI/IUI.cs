using SEF.Repository;
namespace SEF.UI;
public interface IUI {
    public abstract void Init();
    public abstract void Register<Model, Form>(Form form) where Model: class, IModel, new()
                                                          where Form: UIForm<Model>;
    public abstract void Start();
}
