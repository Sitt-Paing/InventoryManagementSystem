import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home-details',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './home-details.html',
  styleUrl: './home-details.scss'
})
export class HomeDetails {}
