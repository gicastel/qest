using System.Threading.Tasks;

namespace qest.Visualizers
{
    public interface IVisualizer
    {
        Task<int> RunAllAsync();
    }
}