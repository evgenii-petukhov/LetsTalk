import { Component, EventEmitter, Input, Output } from '@angular/core';
import { faPaperPlane } from '@fortawesome/free-solid-svg-icons';

@Component({
    selector: 'app-send-button',
    templateUrl: './send-button.component.html',
    styleUrl: './send-button.component.scss',
    standalone: false,
})
export class SendButtonComponent {
    faPaperPlane = faPaperPlane;
    @Input() disabled = false;
    @Output() buttonClick = new EventEmitter();

    onButtonClicked(): void {
        this.buttonClick.emit();
    }
}
