import { Component } from '@angular/core';
import { faRobot } from '@fortawesome/free-solid-svg-icons';

@Component({
    selector: 'app-error',
    templateUrl: './error.component.html',
    styleUrl: './error.component.scss',
    standalone: false,
})
export class ErrorComponent {
    faRobot = faRobot;
}
