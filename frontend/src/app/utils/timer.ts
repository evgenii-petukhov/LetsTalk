export class Timer {
    private timerId: number = null;
    private isTimerExpired = false;

    constructor(
        private readonly callback: () => void,
        private readonly delay: number,
    ) {
        if (typeof callback !== 'function') {
            throw new Error('Callback must be a function');
        }
        if (delay < 0) {
            throw new Error('Delay must be non-negative');
        }

        this.callback = callback;
        this.delay = delay;
        this.start();
    }

    private start(): void {
        if (this.timerId !== null) {
            return;
        }

        this.timerId = window.setTimeout(() => {
            this.isTimerExpired = true;
            this.clear();

            try {
                this.callback();
            } catch (error) {
                console.error(error);
            }
        }, this.delay);
    }

    clear(): void {
        if (this.timerId !== null) {
            clearTimeout(this.timerId);
            this.timerId = null;
        }
    }

    isExpired(): boolean {
        return this.isTimerExpired;
    }

    isActive(): boolean {
        return this.timerId !== null && !this.isTimerExpired;
    }
}
