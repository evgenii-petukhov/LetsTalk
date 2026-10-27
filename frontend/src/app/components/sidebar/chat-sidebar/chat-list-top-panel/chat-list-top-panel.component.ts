import {
    ChangeDetectionStrategy,
    Component,
    computed,
    HostBinding,
    inject,
    Input,
    OnInit,
    signal
} from '@angular/core';
import { StoreService } from '../../../../services/store.service';
import { SessionService } from '../../../../services/session.service';
import { ErrorService } from '../../../../services/error.service';
import { errorMessages } from '../../../../constants/errors';
import { IProfileDto } from '../../../../api-client/api-client';

@Component({
    selector: 'app-chat-list-top-panel',
    templateUrl: './chat-list-top-panel.component.html',
    styleUrls: ['./chat-list-top-panel.component.scss'],
    standalone: false,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChatListTopPanelComponent implements OnInit {
    account = signal<IProfileDto | null>(null);
    @Input()
    @HostBinding('class.navigation-active')
    isNavigationActive: boolean = false;
    urlOptions = computed(() => {
        const account = this.account();
        return account && [account.image, account.photoUrl]
    });

    private readonly storeService = inject(StoreService);
    private readonly sessionService = inject(SessionService);
    private readonly errorService = inject(ErrorService);

    async ngOnInit(): Promise<void> {
        const loggedInUser = await this.storeService.getLoggedInUser();
        this.account.set(loggedInUser);
    }

    async onLogoutButtonClicked(): Promise<void> {
        try {
            await this.sessionService.logout();
            this.sessionService.reloadPage();
        } catch (e) {
            this.errorService.handleError(e, errorMessages.logout);
        }
    }
}
