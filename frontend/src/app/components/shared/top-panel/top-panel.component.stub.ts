import { Component, Input } from '@angular/core';
import { BackButtonStatus } from '../../../models/back-button-status';

@Component({
    selector: 'app-top-panel',
    template: '<div><ng-content></ng-content></div>',
    standalone: false,
})
export class TopPanelStubComponent {
    @Input() backButton: BackButtonStatus;
}