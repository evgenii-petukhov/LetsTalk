import { Component, EventEmitter, Input, Output } from '@angular/core';
import {
    faMicrophone,
    faMicrophoneSlash,
    faVideo,
    faVideoSlash,
} from '@fortawesome/free-solid-svg-icons';

@Component({
    selector: 'app-media-toggle-button',
    templateUrl: './media-toggle-button.component.html',
    styleUrl: './media-toggle-button.component.scss',
    standalone: false,
})
export class MediaToggleButtonComponent {
    private faMicrophoneSlash = faMicrophoneSlash;
    private faMicrophone = faMicrophone;
    private faVideoSlash = faVideoSlash;
    private faVideo = faVideo;

    @Input() mode: 'audio' | 'video' = 'audio';
    @Input() isMuted = false;
    @Output() buttonClick = new EventEmitter<MouseEvent>();

    get activeIcon() {
        return this.mode === 'audio'
            ? this.isMuted
                ? this.faMicrophoneSlash
                : this.faMicrophone
            : this.isMuted
              ? this.faVideoSlash
              : this.faVideo;
    }

    onButtonClicked(e: MouseEvent): void {
        this.buttonClick.emit(e);
    }
}
