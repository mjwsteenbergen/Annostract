using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Annostract.PaperFinders {
    class Actor<T>
    {

        private Task ActorTask;

        private ConcurrentQueue<Task<T>> queue = new ConcurrentQueue<Task<T>>();

        public Actor() {
            ActorTask = Task.Run(async () => {
                while(true)
                {
                    if(queue.TryDequeue(out var queued)){
                        var res = await queued;
                    }
                }
            });
        }

        public void AddTask(Task<T> t)
        {
            queue.Enqueue(t);
        }

    }
}