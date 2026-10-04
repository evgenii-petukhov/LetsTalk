import {
    ChangeDetectionStrategy,
    Component,
    inject,
    Input
} from '@angular/core';
import { Location } from '@angular/common';
import { BackButtonStatus } from '../../../models/back-button-status';

@Component({
    selector: 'app-top-panel',
    templateUrl: './top-panel.component.html',
    styleUrl: './top-panel.component.scss',
    standalone: false,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopPanelComponent {
    @Input() backButton: BackButtonStatus | undefined;
    private readonly location = inject(Location);

    onClick(): void {
        this.location.back();
    }
}
