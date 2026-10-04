import {
    ChangeDetectionStrategy,
    Component
} from '@angular/core';

@Component({
    selector: 'app-privacy-policy',
    templateUrl: './privacy-policy.component.html',
    styleUrls: ['./privacy-policy.component.scss'],
    standalone: false,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrivacyPolicyComponent {}
