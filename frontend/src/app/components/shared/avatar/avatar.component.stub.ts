import { Component, Input } from '@angular/core';
import { IImageDto } from '../../../api-client/api-client';

@Component({
    selector: 'app-avatar',
    template: '<div></div>',
    standalone: false,
})
export class AvatarStubComponent {
    @Input() urlOptions:(string | IImageDto)[];
}
