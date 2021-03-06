﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentNHibernate.AspNet.Identity.Repositories;
using Microsoft.AspNet.Identity;
using Snork.FluentNHibernateTools;

namespace FluentNHibernate.AspNet.Identity
{
    public class FluentNHibernateUserStore<TUser> :
        IUserStore<TUser>,
        IUserLoginStore<TUser>,
        IUserClaimStore<TUser>,
        IUserRoleStore<TUser>,
        IUserPasswordStore<TUser>,
        IUserSecurityStampStore<TUser>,
        IUserEmailStore<TUser>,
        IUserLockoutStore<TUser, string>,
        IUserTwoFactorStore<TUser, string>,
        IUserPhoneNumberStore<TUser>,
        IQueryableUserStore<TUser>
        where TUser : IdentityUser
    {
        private readonly UserClaimRepository<TUser> _userClaimRepository;
        private readonly UserLoginRepository _userLoginRepository;
        private readonly UserRepository<TUser> _userRepository;
        private readonly UserRoleRepository<TUser> _userRoleRepository;


        public FluentNHibernateUserStore(ProviderTypeEnum providerType, string nameOrConnectionString,
            FluentNHibernatePersistenceBuilderOptions options = null)
        {
            _userRepository = new UserRepository<TUser>(providerType, nameOrConnectionString, options);
            _userLoginRepository = new UserLoginRepository(_userRepository.SessionFactoryKey);
            _userClaimRepository = new UserClaimRepository<TUser>(_userRepository.SessionFactoryKey);
            _userRoleRepository = new UserRoleRepository<TUser>(_userRepository.SessionFactoryKey);
        }

        public IQueryable<TUser> Users => _userRepository.GetAll();

        public Task<IList<Claim>> GetClaimsAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            IList<Claim> result = user.Claims.Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToList();
            return Task.FromResult(result);
        }

        public Task AddClaimAsync(TUser user, Claim claim)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (!user.Claims.Any(x => x.ClaimType == claim.Type && x.ClaimValue == claim.Value))
            {
                user.Claims.Add(new IdentityUserClaim
                {
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value
                });

                _userClaimRepository.Insert(user, claim);
            }
            return Task.FromResult(0);
        }

        public Task RemoveClaimAsync(TUser user, Claim claim)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.Claims.RemoveAll(x => x.ClaimType == claim.Type && x.ClaimValue == claim.Value);

            _userClaimRepository.Delete(user, claim);

            return Task.FromResult(0);
        }

        public Task SetEmailAsync(TUser user, string email)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.Email = email;

            return Task.FromResult(0);
        }

        public Task<string> GetEmailAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.EmailConfirmed);
        }

        public Task SetEmailConfirmedAsync(TUser user, bool confirmed)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.EmailConfirmed = confirmed;

            return Task.FromResult(0);
        }

        public Task<TUser> FindByEmailAsync(string email)
        {
            if (email == null)
                throw new ArgumentNullException(nameof(email));

            var user = _userRepository.GetByEmail(email);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                user.Roles = _userRoleRepository.PopulateRoles(user.Id);
                user.Claims = _userClaimRepository.PopulateClaims(user.Id);
                user.Logins = _userLoginRepository.PopulateLogins(user.Id);
                return Task.FromResult(user);
            }
            return Task.FromResult(default(TUser));
        }

        public Task<DateTimeOffset> GetLockoutEndDateAsync(TUser user)
        {
            DateTimeOffset dateTimeOffset;
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (user.LockoutEndDate.HasValue)
            {
                var lockoutEndDateUtc = user.LockoutEndDate;
                dateTimeOffset = new DateTimeOffset(DateTime.SpecifyKind(lockoutEndDateUtc.Value, DateTimeKind.Utc));
            }
            else
            {
                dateTimeOffset = new DateTimeOffset();
            }
            return Task.FromResult(dateTimeOffset);
        }

        public Task SetLockoutEndDateAsync(TUser user, DateTimeOffset lockoutEnd)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            DateTime? value;
            value = lockoutEnd == DateTimeOffset.MinValue ? (DateTime?) null : lockoutEnd.UtcDateTime;
            user.LockoutEndDate = value;
            return Task.FromResult(0);
        }

        public Task<int> IncrementAccessFailedCountAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            user.AccessFailedCount++;
            return Task.FromResult(user.AccessFailedCount);
        }

        public Task ResetAccessFailedCountAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.AccessFailedCount = 0;
            return Task.FromResult(0);
        }

        public Task<int> GetAccessFailedCountAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.AccessFailedCount);
        }

        public Task<bool> GetLockoutEnabledAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.LockoutEnabled);
        }

        public Task SetLockoutEnabledAsync(TUser user, bool enabled)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.LockoutEnabled = enabled;

            return Task.FromResult(0);
        }

        public Task AddLoginAsync(TUser user, UserLoginInfo login)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (login == null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            var id = new UserLoginInfo(login.LoginProvider, login.ProviderKey);
            user.Logins.Add(id);

            _userLoginRepository.Insert(user, login);


            return Task.FromResult(0);
        }

        public Task RemoveLoginAsync(TUser user, UserLoginInfo login)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (login == null)
            {
                throw new ArgumentNullException(nameof(login));
            }
            var tUserLogin = user.Logins.SingleOrDefault(l =>
            {
                if (l.LoginProvider != login.LoginProvider)
                {
                    return false;
                }
                return l.ProviderKey == login.ProviderKey;
            });
            if (tUserLogin != null)
            {
                user.Logins.Remove(tUserLogin);

                _userLoginRepository.Delete(user, login);
            }
            return Task.FromResult(0);
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult<IList<UserLoginInfo>>(user.Logins.ToList());
        }

        public Task<TUser> FindAsync(UserLoginInfo login)
        {
            if (login == null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            var userId = _userLoginRepository.GetByUserLoginInfo(login);

            if (!string.IsNullOrEmpty(userId))
            {
                return FindByIdAsync(userId);
            }
            return Task.FromResult(default(TUser));
        }

        public Task SetPasswordHashAsync(TUser user, string passwordHash)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.PasswordHash = passwordHash;
            return Task.FromResult(0);
        }

        public Task<string> GetPasswordHashAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.PasswordHash != null);
        }

        public Task SetPhoneNumberAsync(TUser user, string phoneNumber)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.PhoneNumber = phoneNumber;

            return Task.FromResult(0);
        }

        public Task<string> GetPhoneNumberAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.PhoneNumber);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.PhoneNumberConfirmed = confirmed;

            return Task.FromResult(0);
        }

        public Task AddToRoleAsync(TUser user, string roleName)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (!user.Roles.Contains(roleName, StringComparer.InvariantCultureIgnoreCase))
            {
                user.Roles.Add(roleName);
            }

            _userRoleRepository.Insert(user, roleName);

            return Task.FromResult(0);
        }

        public Task RemoveFromRoleAsync(TUser user, string roleName)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.Roles.RemoveAll(r => string.Equals(r, roleName, StringComparison.InvariantCultureIgnoreCase));


            _userRoleRepository.Delete(user, roleName);


            return Task.FromResult(0);
        }

        public Task<IList<string>> GetRolesAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult<IList<string>>(user.Roles);
        }

        public Task<bool> IsInRoleAsync(TUser user, string roleName)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.Roles.Contains(roleName, StringComparer.InvariantCultureIgnoreCase));
        }

        public Task SetSecurityStampAsync(TUser user, string stamp)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.SecurityStamp = stamp;
            return Task.FromResult(0);
        }

        public Task<string> GetSecurityStampAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.SecurityStamp);
        }


        public virtual void Dispose()
        {
            // connection is automatically disposed
        }


        public Task CreateAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrEmpty(user.Id))
                throw new InvalidOperationException("user.Id property must be specified before calling CreateAsync");

            _userRepository.Insert(user);
            return Task.FromResult(true);
        }

        public Task DeleteAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            _userRepository.Delete(user);

            return Task.FromResult(true);
        }

        public Task<TUser> FindByIdAsync(string userId)
        {
            var user = _userRepository.GetById(userId);
            if (user != null)
            {
                user.Roles = _userRoleRepository.PopulateRoles(user.Id);
                user.Claims = _userClaimRepository.PopulateClaims(user.Id);
                user.Logins = _userLoginRepository.PopulateLogins(user.Id);
                return Task.FromResult(user);
            }

            return Task.FromResult(default(TUser));
        }

        public Task<TUser> FindByNameAsync(string userName)
        {
            if (userName == null)
            {
                return Task.FromResult(default(TUser));
            }

            var user = _userRepository.GetByName(userName);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                user.Roles = _userRoleRepository.PopulateRoles(user.Id);
                user.Claims = _userClaimRepository.PopulateClaims(user.Id);
                user.Logins = _userLoginRepository.PopulateLogins(user.Id);
                return Task.FromResult(user);
            }

            return Task.FromResult(default(TUser));
        }

        public Task UpdateAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (string.IsNullOrEmpty(user.Id))
                throw new InvalidOperationException("user.Id property must be specified before calling CreateAsync");


            _userRepository.Update(user);

            return Task.FromResult(true);
        }

        public Task SetTwoFactorEnabledAsync(TUser user, bool enabled)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.TwoFactorAuthEnabled = enabled;

            return Task.FromResult(0);
        }

        public Task<bool> GetTwoFactorEnabledAsync(TUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.TwoFactorAuthEnabled);
        }
    }
}