import { IRetryPolicy } from '@microsoft/signalr';

export class ConstantRetryPolicy implements IRetryPolicy {
    // beforeRetry runs each time SignalR asks how long to wait before the next reconnect attempt
    constructor(
        private interval: number,
        private beforeRetry?: () => void,
    ) {
        this.interval = interval;
    }

    nextRetryDelayInMilliseconds(): number {
        this.beforeRetry?.();
        return this.interval;
    }
}
