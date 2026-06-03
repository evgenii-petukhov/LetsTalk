import { Component, EventEmitter, Input, Output } from '@angular/core';
import {
    faCameraRotate,
    faDownLeftAndUpRightToCenter,
} from '@fortawesome/free-solid-svg-icons';

@Component({
    selector: 'app-icon-button',
    templateUrl: './icon-button.component.html',
    styleUrl: './icon-button.component.scss',
    standalone: false,
})
export class IconButtonComponent {
    private faCameraRotate = faCameraRotate;
    private faDownLeftAndUpRightToCenter = faDownLeftAndUpRightToCenter;

    @Input() mode: 'minimize' | 'switch-camera' = 'minimize';
    @Output() buttonClick = new EventEmitter<MouseEvent>();

    get activeIcon() {
        return this.mode === 'minimize'
            ? this.faDownLeftAndUpRightToCenter
            : this.faCameraRotate;
    }

    onButtonClicked(e: MouseEvent): void {
        this.buttonClick.emit(e);
    }
}
