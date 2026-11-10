import { IRetryPolicy, RetryContext } from '@microsoft/signalr';

export class ConstantRetryPolicy implements IRetryPolicy {
    // beforeRetry runs each time SignalR asks how long to wait before the next reconnect attempt
    constructor(
        private interval: number,
        private beforeRetry?: (retryContext: RetryContext) => void,
    ) {
        this.interval = interval;
    }

    nextRetryDelayInMilliseconds(retryContext?: RetryContext): number {
        if (retryContext) {
            this.beforeRetry?.(retryContext);
        }

        return this.interval;
    }
}
