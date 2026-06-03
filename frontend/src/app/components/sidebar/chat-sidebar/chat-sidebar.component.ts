import { Component } from '@angular/core';
import { faCirclePlus } from '@fortawesome/free-solid-svg-icons';

@Component({
    selector: 'app-chat-sidebar',
    templateUrl: './chat-sidebar.component.html',
    styleUrl: './chat-sidebar.component.scss',
    standalone: false,
})
export class ChatSidebarComponent {
    faCirclePlus = faCirclePlus;
}
