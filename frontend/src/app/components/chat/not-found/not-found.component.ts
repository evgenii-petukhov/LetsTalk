import { Component } from '@angular/core';
import { faGhost } from '@fortawesome/free-solid-svg-icons';

@Component({
    selector: 'app-not-found',
    templateUrl: './not-found.component.html',
    styleUrl: './not-found.component.scss',
    standalone: false,
})
export class NotFoundComponent {
    faGhost = faGhost;
}
