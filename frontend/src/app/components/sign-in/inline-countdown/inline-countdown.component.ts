import {
    Component,
    EventEmitter,
    Input,
    OnDestroy,
    OnInit,
    Output,
    signal,
} from '@angular/core';

@Component({
    selector: 'app-inline-countdown',
    templateUrl: './inline-countdown.component.html',
    standalone: false,
})
export class InlineCountdownComponent implements OnInit, OnDestroy {
    @Input() startValue: number;
    value = signal(0);
    @Output() expired = new EventEmitter<void>();
    private timerId = 0;

    ngOnInit(): void {
        this.start();
    }

    ngOnDestroy(): void {
        clearInterval(this.timerId);
    }

    private start(): void {
        this.value.set(this.startValue);
        this.timerId = window.setInterval(() => this.tick(), 1000);
    }

    private stop(): void {
        this.expired.emit();
        window.clearInterval(this.timerId);
    }

    private tick(): void {
        this.value.update(value => value - 1);

        if (this.value() <= 0) {
            this.stop();
        }
    }
}
