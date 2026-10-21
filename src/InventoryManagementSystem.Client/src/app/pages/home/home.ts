import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SharedService } from '../../core/services/shared.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.scss'
})
export class Home {
  protected sharedService = inject(SharedService);
  currentYear = new Date().getFullYear();
  company = {
    name: 'INNO',
    email: 'hello@inno.example',
    phone: '+95 9 XXX XXX XXX',
    address: 'Yangon, Myanmar'
  };
}
