using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface ICinemaRepository
{
    Task<List<Cinema>> GetActiveCinemasAsync(CancellationToken cancellationToken = default);
}