import { Component, Input } from '@angular/core';
import { Message } from '../../../models/message';

@Component({
    selector: 'app-message',
    templateUrl: './message.component.html',
    styleUrls: ['./message.component.scss'],
    standalone: false,
})
export class MessageComponent {
    @Input() message: Message;

    isImageError = false;

    onImageError() {
        this.isImageError = true;
    }
}
