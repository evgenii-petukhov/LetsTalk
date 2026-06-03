import { Component, computed, HostBinding, inject, Input, OnInit, signal } from '@angular/core';
import { StoreService } from '../../../../services/store.service';
import { IProfileDto } from '../../../../api-client/api-client';

@Component({
    selector: 'app-chat-list-top-panel',
    templateUrl: './chat-list-top-panel.component.html',
    styleUrls: ['./chat-list-top-panel.component.scss'],
    standalone: false,
})
export class ChatListTopPanelComponent implements OnInit {
    account = signal<IProfileDto>(null);
    @Input()
    @HostBinding('class.navigation-active')
    isNavigationActive: boolean = false;
    urlOptions = computed(() => {
        const account = this.account();
        return account && [account.image, account.photoUrl]
    });

    private readonly storeService = inject(StoreService);

    async ngOnInit(): Promise<void> {
        const loggedInUser = await this.storeService.getLoggedInUser();
        this.account.set(loggedInUser);
    }

    onLogoutButtonClicked(): void {
        window.localStorage.clear();
        window.location.reload();
    }
}
