import { Component, EventEmitter, Input, Output } from '@angular/core';
import { faChevronLeft } from '@fortawesome/free-solid-svg-icons';

@Component({
    selector: 'app-back-button',
    templateUrl: './back-button.component.html',
    styleUrl: './back-button.component.scss',
    standalone: false,
})
export class BackButtonComponent {
    faChevronLeft = faChevronLeft;
    @Input() showText = false;
    @Output() buttonClick = new EventEmitter();

    onButtonClicked(): void {
        this.buttonClick.emit();
    }
}
