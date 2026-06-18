using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Cinemas;

public interface ICinemaService
{
    Task<List<Cinema>> GetActiveCinemasAsync(CancellationToken cancellationToken = default);
}