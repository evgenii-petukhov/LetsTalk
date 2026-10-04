import {
    ChangeDetectionStrategy,
    Component,
    HostBinding,
    Input
} from '@angular/core';

@Component({
    selector: 'app-account-list-top-panel',
    templateUrl: './account-list-top-panel.component.html',
    styleUrl: './account-list-top-panel.component.scss',
    standalone: false,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountListTopPanelComponent {
    @Input()
    @HostBinding('class.navigation-active')
    isNavigationActive: boolean = false;

    onLogoutButtonClicked(): void {
        window.localStorage.clear();
        window.location.reload();
    }
}
