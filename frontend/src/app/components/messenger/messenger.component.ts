import {
    Component,
    HostListener,
    inject,
    OnDestroy,
    OnInit,
    signal,
} from '@angular/core';
import {
    IChatDto,
    IImagePreviewDto,
    ILinkPreviewDto,
    IMessageDto,
} from '../../api-client/api-client';
import { Store } from '@ngrx/store';
import { StoreService } from '../../services/store.service';
import {
    combineLatest,
    filter,
    map,
    startWith,
    Subject,
    takeUntil,
} from 'rxjs';
import {
    selectIsOngoingCallPanelVisible,
    selectSelectedChat,
} from '../../state/selected-chat/selected-chat.selector';
import { selectChats } from '../../state/chats/chats.selector';
import { SignalrHandlerService } from '../../services/signalr-handler.service';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { IdGeneratorService } from '../../services/id-generator.service';
import { IncomingCall } from '../../models/incoming-call';
import { EstablishConnection } from '../../models/establish-connection';
import { RtcConnectionService } from '../../services/rtc-connection.service';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
    selector: 'app-messenger',
    templateUrl: './messenger.component.html',
    styleUrls: ['./messenger.component.scss'],
    standalone: false,
})
export class MessengerComponent implements OnInit, OnDestroy {
    isSidebarShown = signal(false);

    private chats: readonly IChatDto[] = [];
    private selectedChat: IChatDto | null = null;
    private isWindowActive = true;

    private readonly unsubscribe$: Subject<void> = new Subject<void>();
    private readonly store = inject(Store);
    private readonly storeService = inject(StoreService);
    private readonly signalrHandlerService = inject(SignalrHandlerService);
    private readonly activatedRoute = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly idGeneratorService = inject(IdGeneratorService);
    private readonly rtcConnectionService = inject(RtcConnectionService);

    @HostListener('document:visibilitychange', ['$event'])
    onVisibilityChange(event: Event): void {
        this.isWindowActive = !(event.target as Document).hidden;
        this.storeService.markAllAsRead(this.selectedChat);
    }

    isOngoingCallPanelVisible = toSignal(
        this.store.select(selectIsOngoingCallPanelVisible),
    );

    async ngOnInit(): Promise<void> {
        await this.storeService.initChatStorage();

        this.router.events
            .pipe(
                filter((event) => event instanceof NavigationEnd),
                startWith(null),
                map(
                    () => this.activatedRoute.firstChild?.snapshot.params || {},
                ),
                takeUntil(this.unsubscribe$),
            )
            .subscribe((params) => {
                const chatId = params['id'];
                this.isSidebarShown.set(!chatId);
                this.storeService.setSelectedChatId(chatId);

                if (this.chats && !this.idGeneratorService.isFake(chatId)) {
                    this.storeService.markAllAsRead(
                        this.chats.find((c) => c.id === chatId),
                    );
                }
            });

        combineLatest([
            this.store.select(selectChats),
            this.store.select(selectSelectedChat),
        ])
            .pipe(takeUntil(this.unsubscribe$))
            .subscribe(([chats, chat]) => {
                this.chats = chats;
                this.selectedChat = chat;
            });

        await this.signalrHandlerService.initHandlers(
            this.handleMessageNotification.bind(this),
            this.handleLinkPreviewNotification.bind(this),
            this.handleImagePreviewNotification.bind(this),
            this.handleIncomingCallNotification.bind(this),
            this.handleEstablishConnectionNotification.bind(this),
        );
    }

    ngOnDestroy(): void {
        this.unsubscribe$.next();
        this.unsubscribe$.complete();
        this.signalrHandlerService.removeHandlers();
    }

    handleMessageNotification(dto: IMessageDto): void {
        this.signalrHandlerService.handleMessageNotification(
            dto,
            this.selectedChat?.id,
            this.chats,
            this.isWindowActive,
        );
    }

    handleLinkPreviewNotification(dto: ILinkPreviewDto): void {
        this.signalrHandlerService.handleLinkPreviewNotification(
            dto,
            this.selectedChat?.id,
        );
    }

    handleImagePreviewNotification(dto: IImagePreviewDto): void {
        this.signalrHandlerService.handleImagePreviewNotification(
            dto,
            this.selectedChat?.id,
        );
    }

    async handleIncomingCallNotification(data: IncomingCall): Promise<void> {
        this.signalrHandlerService.handleIncomingCallNotification(
            this.chats,
            data.callId,
            data.chatId,
            data.offer,
        );
    }

    handleEstablishConnectionNotification(data: EstablishConnection): void {
        this.rtcConnectionService.establishConnection(data.answer);
    }
}
