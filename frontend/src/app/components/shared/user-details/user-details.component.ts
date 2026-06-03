import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-user-details',
    templateUrl: './user-details.component.html',
    styleUrl: './user-details.component.scss',
    standalone: false,
})
export class UserDetailsComponent {
    @Input() value: string;
}
