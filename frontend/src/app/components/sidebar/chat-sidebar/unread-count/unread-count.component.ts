import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-unread-count',
    templateUrl: './unread-count.component.html',
    styleUrl: './unread-count.component.scss',
    standalone: false,
})
export class UnreadCountComponent {
    @Input() value: number;
}
