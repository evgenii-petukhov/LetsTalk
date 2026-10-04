import {
    ChangeDetectionStrategy,
    Component,
    Input,
    signal
} from '@angular/core';
import { Message } from '../../../models/message';

@Component({
    selector: 'app-message',
    templateUrl: './message.component.html',
    styleUrls: ['./message.component.scss'],
    standalone: false,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MessageComponent {
    @Input() message: Message | undefined;

    isImageError = signal(false);

    onImageError() {
        this.isImageError.set(true);
    }
}
