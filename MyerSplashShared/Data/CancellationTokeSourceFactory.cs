using System.Threading;

namespace MyerSplashShared.Data
{
    public abstract class CancellationTokenSourceFactory
    {
        public static CancellationTokenSourceFactory CreateDefault(int cancelDelayMillis)
        {
            return new DefaultCancellationTokeSourceFactory(cancelDelayMillis);
        }

        public static CancellationTokenSourceFactory CreateDefault()
        {
            return new DefaultCancellationTokeSourceFactory();
        }

        public abstract CancellationTokenSource Create();
    }

    public class DefaultCancellationTokeSourceFactory : CancellationTokenSourceFactory
    {
        private readonly int _cancelDelayMillis = 5000;

        internal DefaultCancellationTokeSourceFactory(int cancelDelayMillis = 5000)
        {
            _cancelDelayMillis = cancelDelayMillis;
        }

        public override CancellationTokenSource Create()
        {
            return new CancellationTokenSource(_cancelDelayMillis);
        }
    }

}
