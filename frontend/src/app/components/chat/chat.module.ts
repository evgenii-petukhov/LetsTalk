import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SelectImageButtonComponent } from './select-image-button/select-image-button.component';
import { SendButtonComponent } from './send-button/send-button.component';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { ComposeAreaComponent } from './compose-area/compose-area.component';
import { FormsModule } from '@angular/forms';
import { ChatHeaderComponent } from './chat-header/chat-header.component';
import { SharedModule } from '../shared/shared.module';
import { MessageComponent } from './message/message.component';
import { ImageComponent } from './image/image.component';
import { ChatComponent } from './chat.component';
import { VisibleOnlyPipe } from '../../pipes/visibleOnly';
import { AppRoutingModule } from '../../app-routing.module';
import { AutoResizeTextAreaComponent } from './auto-resize-text-area/auto-resize-text-area.component';
import { MessageListComponent } from './message-list/message-list.component';
import { NotFoundComponent } from './not-found/not-found.component';
import { ErrorComponent } from './error/error.component';
import { VideoCallComponent } from './video-call/video-call.component';
import { CallButtonComponent } from './call-button/call-button.component';
import { MediaToggleButtonComponent } from './media-toggle-button/media-toggle-button.component';
import { IncomingCallComponent } from './incoming-call/incoming-call.component';
import { OngoingCallComponent } from './ongoing-call-panel/ongoing-call-panel.component';
import { IconButtonComponent } from './icon-button/icon-button.component';

@NgModule({
    declarations: [
        SelectImageButtonComponent,
        SendButtonComponent,
        ComposeAreaComponent,
        ChatHeaderComponent,
        MessageComponent,
        ImageComponent,
        ChatComponent,
        VideoCallComponent,
        AutoResizeTextAreaComponent,
        MessageListComponent,
        NotFoundComponent,
        ErrorComponent,
        CallButtonComponent,
        MediaToggleButtonComponent,
        VisibleOnlyPipe,
        IncomingCallComponent,
        OngoingCallComponent,
        IconButtonComponent,
    ],
    imports: [
        CommonModule,
        FontAwesomeModule,
        FormsModule,
        SharedModule,
        AppRoutingModule,
    ],
    exports: [
        ChatComponent,
        OngoingCallComponent,
    ],
})
export class ChatModule {}
