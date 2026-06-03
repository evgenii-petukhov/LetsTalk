import { Component, EventEmitter, Output } from '@angular/core';
import { faRightFromBracket } from '@fortawesome/free-solid-svg-icons';

@Component({
    selector: 'app-logout-button',
    templateUrl: './logout-button.component.html',
    styleUrl: './logout-button.component.scss',
    standalone: false,
})
export class LogoutButtonComponent {
    faRightFromBracket = faRightFromBracket;
    @Output() buttonClick = new EventEmitter();

    onButtonClicked(): boolean {
        this.buttonClick.emit();
        return false;
    }
}
