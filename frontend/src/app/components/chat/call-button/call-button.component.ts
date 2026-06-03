import { Component, EventEmitter, Input, Output } from '@angular/core';
import { faPhone } from '@fortawesome/free-solid-svg-icons';

@Component({
    selector: 'app-call-button',
    templateUrl: './call-button.component.html',
    styleUrl: './call-button.component.scss',
    standalone: false,
})
export class CallButtonComponent {
    faPhone = faPhone;
    @Input() mode: 'start-call' | 'accept-call' | 'end-call' = 'start-call';
    @Input() text: string;
    @Output() buttonClick = new EventEmitter<MouseEvent>();

    onButtonClicked(event: MouseEvent): void {
        this.buttonClick.emit(event);
    }
}
