import { IRetryPolicy } from '@microsoft/signalr';

export class ConstantRetryPolicy implements IRetryPolicy {
    constructor(private interval: number) {
        this.interval = interval;
    }

    nextRetryDelayInMilliseconds(): number {
        return this.interval;
    }
}
