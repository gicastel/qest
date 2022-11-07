using System.Threading.Tasks;
using qest.Models;

public interface IConnector 
{    
    void SetConnectionString(string connectionString);
    public Task LoadData(Test test);
}